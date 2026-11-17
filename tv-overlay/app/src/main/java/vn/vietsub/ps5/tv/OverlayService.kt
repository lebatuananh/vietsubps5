package vn.vietsub.ps5.tv

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.graphics.PixelFormat
import android.os.Handler
import android.os.IBinder
import android.os.Looper
import android.os.SystemClock
import android.provider.Settings
import android.util.Log
import android.view.View
import android.view.WindowManager
import androidx.core.app.ServiceCompat

/**
 * Service foreground (specialUse) của bản sản phẩm: giữ server TCP 47820 + cửa sổ overlay.
 * Thứ tự an toàn: thêm cửa sổ overlay TRƯỚC, rồi startForeground (discovery D7). Bỏ tự tắt 30 phút của APK kiểm tra:
 * lớp phủ rỗng khi lõi không gửi (ttl). `START_NOT_STICKY`.
 */
class OverlayService : Service() {
    private val main = Handler(Looper.getMainLooper())
    private val model = OverlayModel { Log.w(TAG, it) }
    private var view: OverlayView? = null
    private var server: OverlayServer? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onCreate() {
        super.onCreate()
        instance = this
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        if (intent?.action == ACTION_STOP) {
            shutdown()
            return START_NOT_STICKY
        }
        val canDraw = Settings.canDrawOverlays(this)
        val added = canDraw && (view != null || addOverlay())
        // startForegroundService bắt buộc có startForeground; không có quyền thì vẫn gọi rồi dừng ngay.
        ServiceCompat.startForeground(this, NOTIF_ID, buildNotification(), ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)
        if (!added) {
            Log.w(TAG, "không có quyền SYSTEM_ALERT_WINDOW hoặc addView thất bại — dừng")
            shutdown()
            return START_NOT_STICKY
        }
        startServer()
        AppState.running = true
        applyOverlayEnabled()
        return START_NOT_STICKY
    }

    private fun startServer() {
        if (server != null) return
        val s = OverlayServer(Protocol.PORT, { PairingStore.code(this) }, object : OverlayServer.Listener {
            override fun onFrame(frame: OverlayFrame) {
                main.post {
                    model.applyFrame(frame, SystemClock.uptimeMillis())
                    view?.onModelChanged()
                }
            }

            override fun onConnectionChanged(connected: Boolean) {
                AppState.connected = connected
                if (connected) { AppState.everConnected = true; AppState.codeChanged = false; Log.i(TAG, "engine đã kết nối") }
                else { AppState.lastLossAt = SystemClock.uptimeMillis(); Log.i(TAG, "engine ngắt kết nối") }
            }
        }, { Log.i(TAG, it) })
        try {
            s.start()
            server = s
            AppState.serverError = null
            Log.i(TAG, "server nghe cổng ${Protocol.PORT}")
        } catch (e: Exception) {
            AppState.serverError = e.message ?: e.javaClass.simpleName
            Log.e(TAG, "không mở được cổng ${Protocol.PORT}: $e")
        }
    }

    private fun addOverlay(): Boolean {
        val wm = getSystemService(Context.WINDOW_SERVICE) as WindowManager
        val lp = WindowManager.LayoutParams(
            WindowManager.LayoutParams.MATCH_PARENT,
            WindowManager.LayoutParams.MATCH_PARENT,
            WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY,
            WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE or
                WindowManager.LayoutParams.FLAG_NOT_TOUCHABLE or
                WindowManager.LayoutParams.FLAG_LAYOUT_NO_LIMITS or
                WindowManager.LayoutParams.FLAG_LAYOUT_IN_SCREEN,
            PixelFormat.TRANSLUCENT,
        ).apply { layoutInDisplayCutoutMode = WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES }
        val v = OverlayView(this, model)
        return try {
            wm.addView(v, lp)
            view = v
            true
        } catch (e: WindowManager.BadTokenException) {
            Log.e(TAG, "addView BadTokenException: $e"); false
        } catch (e: SecurityException) {
            Log.e(TAG, "addView SecurityException: $e"); false
        }
    }

    private fun applyOverlayEnabled() {
        view?.visibility = if (AppState.overlayOn && !AppState.appInForeground) View.VISIBLE else View.GONE
        view?.onModelChanged()
    }

    /** Bật/tắt vẽ lớp phủ (server vẫn nhận gói). Gọi trên luồng chính. */
    fun setOverlayEnabled(on: Boolean) {
        AppState.overlayOn = on
        applyOverlayEnabled()
    }

    /** App ra/vào tiền cảnh: ẩn lớp phủ khi MainActivity đang hiện (NB7). Gọi trên luồng chính. */
    fun setAppForeground(fg: Boolean) {
        AppState.appInForeground = fg
        applyOverlayEnabled()
    }

    /** "Ghép cặp lại"/"Tạo mã mới": SINH mã mới MỘT lần ở đây, ngắt kết nối hiện tại (engine cũ bị từ chối ở lần hello sau). */
    fun repair() {
        PairingStore.regenerate(this)
        AppState.codeChanged = true
        AppState.everConnected = false
        AppState.connected = false
        server?.disconnectCurrent()
    }

    private fun removeOverlay() {
        view?.let {
            val wm = getSystemService(Context.WINDOW_SERVICE) as WindowManager
            try { wm.removeView(it) } catch (e: IllegalArgumentException) { Log.w(TAG, "removeView: $e") }
        }
        view = null
    }

    private fun shutdown() {
        server?.stop(); server = null
        removeOverlay()
        AppState.running = false
        AppState.connected = false
        stopForeground(STOP_FOREGROUND_REMOVE)
        stopSelf()
    }

    override fun onDestroy() {
        server?.stop(); server = null
        removeOverlay()
        AppState.running = false
        AppState.connected = false
        if (instance === this) instance = null
        super.onDestroy()
    }

    private fun buildNotification(): Notification {
        val nm = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        nm.createNotificationChannel(NotificationChannel(CHANNEL_ID, getString(R.string.notif_channel), NotificationManager.IMPORTANCE_MIN))
        return Notification.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_stat)
            .setContentTitle(getString(R.string.notif_title))
            .setOngoing(true)
            .build()
    }

    companion object {
        const val TAG = "VietSubOverlay"
        const val ACTION_STOP = "vn.vietsub.ps5.tv.STOP"
        private const val CHANNEL_ID = "overlay"
        private const val NOTIF_ID = 1

        @Volatile var instance: OverlayService? = null

        val running: Boolean get() = AppState.running
    }
}
