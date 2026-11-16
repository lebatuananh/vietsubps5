package vn.vietsub.ps5.tv

import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.content.res.ColorStateList
import android.graphics.Color
import android.graphics.drawable.GradientDrawable
import android.graphics.drawable.StateListDrawable
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.PowerManager
import android.provider.Settings
import android.util.TypedValue
import android.view.Gravity
import android.view.View
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import androidx.core.content.ContextCompat
import androidx.window.layout.WindowMetricsCalculator

/**
 * Màn tối giản — CÔNG CỤ KIỂM TRA Phase 1, không phải UI sản phẩm (sẽ bị thay hoàn toàn ở Phase 3 theo mockup Phase 2).
 * Chữ tiếng Việt; trạng thái quyền làm mới ở onResume; focus D-pad mặc định vào nút của việc tiếp theo chưa xong.
 */
class MainActivity : android.app.Activity() {
    private lateinit var overlayStatus: TextView
    private lateinit var batteryStatus: TextView
    private lateinit var runStatus: TextView
    private lateinit var metrics: TextView
    private lateinit var hint: TextView
    private lateinit var grantBtn: Button
    private lateinit var batteryBtn: Button
    private lateinit var toggleBtn: Button

    /** Nút vừa bấm mà việc chưa xong; kiểm lại ở onResume khi User quay về (B1). */
    private enum class Pending { OVERLAY, BATTERY }
    private var pending: Pending? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        buildUi()
        handleDebugExtras(intent)
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        handleDebugExtras(intent)
    }

    override fun onResume() {
        super.onResume()
        refresh()
        checkPendingAfterReturn()
        ticker.run()
    }

    override fun onPause() {
        toggleBtn.removeCallbacks(ticker)
        super.onPause()
    }

    /** Làm mới nhãn "đang bật/tắt" mỗi giây khi màn đang hiện (service có thể tự tắt); KHÔNG đụng focus. */
    private val ticker = object : Runnable {
        override fun run() {
            val running = OverlayService.running
            val status = getString(if (running) R.string.overlay_running else R.string.overlay_stopped)
            val toggle = getString(if (running) R.string.btn_toggle_off else R.string.btn_toggle_on)
            // Chỉ đặt khi đổi: setText liên tục làm uiautomator không bao giờ "idle" (dump lỗi).
            if (runStatus.text.toString() != status) runStatus.text = status
            if (toggleBtn.text.toString() != toggle) toggleBtn.text = toggle
            toggleBtn.postDelayed(this, 1000)
        }
    }

    private fun buildUi() {
        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setBackgroundColor(Color.parseColor("#14202C"))
            val p = dp(48)
            setPadding(p, p, p, p)
        }
        fun text(sizeSp: Float, bold: Boolean = false) = TextView(this).apply {
            setTextColor(Color.WHITE)
            setTextSize(TypedValue.COMPLEX_UNIT_SP, sizeSp)
            if (bold) setTypeface(typeface, android.graphics.Typeface.BOLD)
            setPadding(0, dp(6), 0, dp(6))
        }
        root.addView(text(28f, true).apply { setText(R.string.title) })
        root.addView(text(16f).apply { setText(R.string.subtitle) })
        overlayStatus = text(20f).also { root.addView(it) }
        batteryStatus = text(20f).also { root.addView(it) }
        runStatus = text(20f).also { root.addView(it) }
        metrics = text(16f).also { root.addView(it) }
        hint = text(16f).apply {
            setTextColor(Color.parseColor("#FFD400"))
            visibility = View.GONE
        }.also { root.addView(it) }

        fun button(textRes: Int, onClick: () -> Unit) = Button(this).apply {
            setText(textRes)
            isFocusable = true
            styleForTv(this)
            setOnClickListener { onClick() }
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT, LinearLayout.LayoutParams.WRAP_CONTENT,
            ).apply { topMargin = dp(12); gravity = Gravity.START }
        }
        grantBtn = button(R.string.btn_grant_overlay) { requestOverlayPermission() }.also { root.addView(it) }
        batteryBtn = button(R.string.btn_battery) { requestBatteryExemption() }.also { root.addView(it) }
        toggleBtn = button(R.string.btn_toggle_on) { toggleOverlay() }.also { root.addView(it) }
        // Hint dự phòng làm nội dung dài hơn màn: bọc ScrollView để nút không bị ép mất chữ; D-pad focus tự cuộn tới nút.
        setContentView(
            android.widget.ScrollView(this).apply {
                setBackgroundColor(Color.parseColor("#14202C"))
                isFillViewport = true
                addView(root)
            },
        )
    }

    private fun refresh() {
        val canDraw = Settings.canDrawOverlays(this)
        val ignoring = (getSystemService(Context.POWER_SERVICE) as PowerManager).isIgnoringBatteryOptimizations(packageName)
        val running = OverlayService.running
        overlayStatus.setText(if (canDraw) R.string.perm_overlay_yes else R.string.perm_overlay_no)
        batteryStatus.setText(if (ignoring) R.string.battery_yes else R.string.battery_no)
        runStatus.setText(if (running) R.string.overlay_running else R.string.overlay_stopped)
        toggleBtn.setText(if (running) R.string.btn_toggle_off else R.string.btn_toggle_on)

        val b = WindowMetricsCalculator.getOrCreate().computeCurrentWindowMetrics(this).bounds
        val dm = resources.displayMetrics
        metrics.text = getString(
            R.string.metrics, b.width(), b.height(), dm.density, dm.densityDpi, Build.VERSION.RELEASE, Build.VERSION.SDK_INT,
        )

        // Focus mặc định vào nút của việc tiếp theo chưa xong.
        val next = when {
            !canDraw -> grantBtn
            !ignoring -> batteryBtn
            else -> toggleBtn
        }
        next.requestFocus()
    }

    /** B2: focus nhìn thấy rõ — focus = nền vàng #FFD400 + chữ đen; không focus = nền xám đậm + chữ trắng. */
    private fun styleForTv(b: Button) {
        fun bg(color: Int) = GradientDrawable().apply { setColor(color); cornerRadius = dp(8).toFloat() }
        b.background = StateListDrawable().apply {
            addState(intArrayOf(android.R.attr.state_focused), bg(Color.parseColor("#FFD400")))
            addState(intArrayOf(android.R.attr.state_pressed), bg(Color.parseColor("#FFD400")))
            addState(intArrayOf(), bg(Color.parseColor("#3A4654")))
        }
        b.setTextColor(
            ColorStateList(
                arrayOf(intArrayOf(android.R.attr.state_focused), intArrayOf(android.R.attr.state_pressed), intArrayOf()),
                intArrayOf(Color.BLACK, Color.BLACK, Color.WHITE),
            ),
        )
        b.setPadding(dp(24), dp(12), dp(24), dp(12))
    }

    /** Intent của Settings trên Google TV có thể phân giải sang stub rỗng (EmptyStubActivity) — coi như KHÔNG có màn. */
    private fun opensRealScreen(i: Intent): Boolean {
        val ri = packageManager.resolveActivity(i, 0) ?: return true // không thấy được ≠ không có: để startActivity quyết
        return !ri.activityInfo.name.contains("EmptyStub", ignoreCase = true)
    }

    private fun launchSettings(i: Intent, which: Pending, fallbackRes: Int) {
        pending = which
        if (!opensRealScreen(i)) {
            pending = null
            showHint(fallbackRes)
            return
        }
        try {
            startActivity(i)
        } catch (e: ActivityNotFoundException) {
            pending = null
            showHint(fallbackRes)
        }
    }

    private fun requestOverlayPermission() {
        showHint(R.string.hint_overlay_list) // N2: màn cấp quyền là danh sách chung, báo User tìm dòng nào
        launchSettings(
            Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:$packageName")),
            Pending.OVERLAY, R.string.fallback_overlay,
        )
    }

    private fun requestBatteryExemption() {
        launchSettings(
            Intent(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS, Uri.parse("package:$packageName")),
            Pending.BATTERY, R.string.fallback_battery,
        )
    }

    /** B1: sau khi User quay về, nếu việc vừa bấm vẫn chưa xong ⇒ hiện lệnh dự phòng (không dựa vào ActivityNotFoundException). */
    private fun checkPendingAfterReturn() {
        val p = pending ?: return
        pending = null
        val canDraw = Settings.canDrawOverlays(this)
        val ignoring = (getSystemService(Context.POWER_SERVICE) as PowerManager).isIgnoringBatteryOptimizations(packageName)
        when (p) {
            Pending.OVERLAY -> if (!canDraw) showHint(R.string.fallback_overlay) else hint.visibility = View.GONE
            Pending.BATTERY -> if (!ignoring) showHint(R.string.fallback_battery) else hint.visibility = View.GONE
        }
    }

    private fun toggleOverlay() {
        if (OverlayService.running) stopOverlay() else startOverlay(null)
        refresh()
    }

    private fun startOverlay(autoOffMs: Long?) {
        if (!Settings.canDrawOverlays(this)) {
            showHint(R.string.need_overlay)
            return
        }
        val i = Intent(this, OverlayService::class.java)
        if (autoOffMs != null) i.putExtra(OverlayService.EXTRA_AUTO_OFF_MS, autoOffMs)
        ContextCompat.startForegroundService(this, i)
        hint.visibility = View.GONE
        // OverlayService.running được set trong service; cập nhật nhãn sau một nhịp.
        toggleBtn.postDelayed({ if (!isDestroyed) refresh() }, 500)
    }

    private fun stopOverlay() {
        startService(Intent(this, OverlayService::class.java).setAction(OverlayService.ACTION_STOP))
    }

    /** Chỉ bản debug (N20): `am start -n vn.vietsub.ps5.tv/.MainActivity --ez overlay_on true [--el auto_off_ms 8000]`. */
    private fun handleDebugExtras(i: Intent?) {
        if (!BuildConfig.DEBUG || i == null || !i.hasExtra("overlay_on")) return
        if (i.getBooleanExtra("overlay_on", false)) {
            startOverlay(if (i.hasExtra(OverlayService.EXTRA_AUTO_OFF_MS)) i.getLongExtra(OverlayService.EXTRA_AUTO_OFF_MS, 0L) else null)
        } else {
            stopOverlay()
        }
    }

    private fun showHint(res: Int) {
        hint.setText(res)
        hint.visibility = View.VISIBLE
    }

    private fun dp(v: Int) = (v * resources.displayMetrics.density).toInt()
}
