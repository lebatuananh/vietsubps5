package vn.vietsub.ps5.tv

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.graphics.Paint
import android.graphics.PixelFormat
import android.os.Handler
import android.os.IBinder
import android.os.Looper
import android.provider.Settings
import android.util.Log
import android.view.WindowManager
import androidx.core.app.ServiceCompat

/**
 * Service foreground giữ cửa sổ overlay. Thứ tự: thêm cửa sổ overlay TRƯỚC, rồi mới startForeground.
 * START_NOT_STICKY; tự tắt sau [DEFAULT_AUTO_OFF_MS] (bản debug có thể rút ngắn qua extra auto_off_ms).
 */
class OverlayService : Service() {
    private var view: OverlayView? = null
    private val handler = Handler(Looper.getMainLooper())
    private val autoOff = Runnable {
        Log.i(TAG, "tự tắt sau hạn — gỡ cửa sổ + dừng service")
        shutdown()
    }

    override fun onBind(intent: Intent?): IBinder? = null

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
        running = true
        val autoOffMs = if (BuildConfig.DEBUG) {
            intent?.getLongExtra(EXTRA_AUTO_OFF_MS, DEFAULT_AUTO_OFF_MS) ?: DEFAULT_AUTO_OFF_MS
        } else {
            DEFAULT_AUTO_OFF_MS
        }
        handler.removeCallbacks(autoOff)
        handler.postDelayed(autoOff, autoOffMs)
        Log.i(TAG, "overlay bật; tự tắt sau $autoOffMs ms")
        if (BuildConfig.DEBUG) logGlyphCoverage()
        return START_NOT_STICKY
    }

    /** N1: addView có thể ném BadTokenException/SecurityException (quyền bị thu hồi giữa canDrawOverlays và addView) ⇒ false, không crash. */
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
        ).apply {
            layoutInDisplayCutoutMode = WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES
        }
        val v = OverlayView(this)
        return try {
            wm.addView(v, lp)
            view = v
            true
        } catch (e: WindowManager.BadTokenException) {
            Log.e(TAG, "addView BadTokenException: $e")
            false
        } catch (e: SecurityException) {
            Log.e(TAG, "addView SecurityException: $e")
            false
        }
    }

    private fun removeOverlay() {
        view?.let {
            val wm = getSystemService(Context.WINDOW_SERVICE) as WindowManager
            try {
                wm.removeView(it)
            } catch (e: IllegalArgumentException) {
                Log.w(TAG, "removeView: $e")
            }
        }
        view = null
    }

    private fun shutdown() {
        handler.removeCallbacks(autoOff)
        removeOverlay()
        running = false
        stopForeground(STOP_FOREGROUND_REMOVE)
        stopSelf()
    }

    override fun onDestroy() {
        handler.removeCallbacks(autoOff)
        removeOverlay()
        running = false
        super.onDestroy()
    }

    private fun buildNotification(): Notification {
        val nm = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        nm.createNotificationChannel(
            NotificationChannel(CHANNEL_ID, getString(R.string.notif_channel), NotificationManager.IMPORTANCE_MIN),
        )
        return Notification.Builder(this, CHANNEL_ID)
            .setSmallIcon(R.drawable.ic_stat)
            .setContentTitle(getString(R.string.notif_title))
            .setOngoing(true)
            .build()
    }

    /** Chỉ bản debug (1.2 #8): Paint.hasGlyph cho dải U+1EA0–U+1EF9 + chuỗi mẫu; kèm ký tự đối chứng ngoài font. */
    private fun logGlyphCoverage() {
        val paint = Paint().apply { typeface = resources.getFont(R.font.be_vietnam_pro_regular) }
        val missing = ArrayList<String>()
        var ok = 0
        for (cp in 0x1EA0..0x1EF9) {
            val s = String(Character.toChars(cp))
            if (paint.hasGlyph(s)) ok++ else missing.add("U+%04X".format(cp))
        }
        Log.i(GLYPH_TAG, "range U+1EA0-U+1EF9: total=${0x1EF9 - 0x1EA0 + 1} ok=$ok missing=${missing.size} $missing")
        val sampleMissing = getString(R.string.overlay_sample).codePoints().distinct().toArray()
            .filter { !Character.isWhitespace(it) }
            .map { String(Character.toChars(it)) }
            .filter { !paint.hasGlyph(it) }
        Log.i(GLYPH_TAG, "sample string missing glyphs: $sampleMissing")
        // Đối chứng: ký tự chắc chắn KHÔNG có trong Be Vietnam Pro. true ⇒ hasGlyph bị fallback hệ thống làm nhiễu.
        val control = listOf("日", "한").associateWith { paint.hasGlyph(it) }
        Log.i(GLYPH_TAG, "control (CJK, expect false): $control")
    }

    companion object {
        const val TAG = "VietSubOverlay"
        const val GLYPH_TAG = "VietSubGlyph"
        const val ACTION_STOP = "vn.vietsub.ps5.tv.STOP"
        const val EXTRA_AUTO_OFF_MS = "auto_off_ms"
        const val DEFAULT_AUTO_OFF_MS = 30L * 60 * 1000
        private const val CHANNEL_ID = "overlay"
        private const val NOTIF_ID = 1

        /** Trạng thái trong tiến trình: tiến trình bị giết ⇒ false (đúng thực tế vì cửa sổ cũng mất). */
        @Volatile
        var running = false
    }
}
