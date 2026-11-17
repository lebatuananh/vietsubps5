package vn.vietsub.ps5.tv

import android.animation.ValueAnimator
import android.content.ActivityNotFoundException
import android.content.Context
import android.content.Intent
import android.content.res.ColorStateList
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.graphics.drawable.StateListDrawable
import android.net.Uri
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.PowerManager
import android.os.SystemClock
import android.provider.Settings
import android.text.Spannable
import android.text.SpannableString
import android.text.style.ForegroundColorSpan
import android.util.TypedValue
import android.view.Gravity
import android.view.View
import android.widget.Button
import android.widget.FrameLayout
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.TextView
import androidx.core.content.ContextCompat

/**
 * Màn sản phẩm (D13, theo mockup tv-app): Cần cấp quyền → Chờ máy tính (IP:47820 + mã) → Đã kết nối → Mất kết nối;
 * mở app khi lớp phủ đang bật ⇒ TẮT lớp phủ + màn "Đã tắt lớp phủ". Focus mặc định CHỈ đặt khi một màn mở ra
 * (không bị kéo khi làm mới); focus luôn nhìn thấy (vàng #FFD400 + chữ đen).
 */
class MainActivity : android.app.Activity() {
    private enum class Screen { PERMISSION, WAITING, CONNECTED, LOST, OFF }
    private enum class Pending { OVERLAY, BATTERY }

    private val ui = Handler(Looper.getMainLooper())
    private lateinit var holder: FrameLayout
    private lateinit var regular: Typeface
    private lateinit var semibold: Typeface
    private var u = 1f // quy đổi px của khung 1080 → px thật

    private var screen: Screen? = null
    private var pending: Pending? = null
    private var hintText: TextView? = null
    private var hintRes = 0
    private var codeView: TextView? = null
    private var addrView: TextView? = null
    private var codeChangedView: TextView? = null
    private var overlayChip: TextView? = null
    private var batteryChip: TextView? = null
    private var installerBody: TextView? = null
    private var installerToggle: Button? = null

    private val ticker = object : Runnable {
        override fun run() {
            refresh()
            ui.postDelayed(this, 500)
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        if (BuildConfig.DEBUG && intent?.getBooleanExtra("diag", false) == true) {
            // Màn kiểm tra Phase 1 (debug-only, lớp trong src/debug — gọi theo tên để bản release không phụ thuộc)
            startActivity(Intent().setClassName(packageName, "vn.vietsub.ps5.tv.DiagActivity"))
            finish()
            return
        }
        regular = resources.getFont(R.font.be_vietnam_pro_regular)
        semibold = resources.getFont(R.font.be_vietnam_pro_semibold)
        u = resources.displayMetrics.heightPixels / 1080f
        holder = FrameLayout(this)
        holder.background = GradientDrawable(
            GradientDrawable.Orientation.TL_BR, intArrayOf(Color.parseColor("#1A2636"), Color.parseColor("#0D131B"), Color.parseColor("#0A0F15")),
        )
        setContentView(holder)
        ensureService()
        openedByUser()
    }

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        setIntent(intent)
        if (BuildConfig.DEBUG && intent.getBooleanExtra("diag", false)) {
            startActivity(Intent().setClassName(packageName, "vn.vietsub.ps5.tv.DiagActivity"))
            return
        }
        openedByUser()
    }

    override fun onRestart() {
        super.onRestart()
        openedByUser()
    }

    override fun onResume() {
        super.onResume()
        if (!::holder.isInitialized) return
        checkPendingAfterReturn()
        ensureService()
        OverlayService.instance?.setAppForeground(true)
        AppState.appInForeground = true
        ui.removeCallbacks(ticker)
        ticker.run()
    }

    override fun onPause() {
        ui.removeCallbacks(ticker)
        OverlayService.instance?.setAppForeground(false)
        AppState.appInForeground = false
        super.onPause()
    }

    /** Mở app khi lớp phủ đang bật = TẮT lớp phủ (X5-3). Lần mở đầu (service chưa chạy) thì không. */
    private fun openedByUser() {
        val svc = OverlayService.instance
        if (svc != null && AppState.running && AppState.overlayOn) svc.setOverlayEnabled(false)
    }

    private fun ensureService() {
        if (Settings.canDrawOverlays(this) && !AppState.running && OverlayService.instance == null) {
            ContextCompat.startForegroundService(this, Intent(this, OverlayService::class.java))
        }
    }

    // ------------------------------------------------------------------ trạng thái → màn

    private fun computeScreen(): Screen {
        val canDraw = Settings.canDrawOverlays(this)
        return when {
            !canDraw -> Screen.PERMISSION
            AppState.running && !AppState.overlayOn -> Screen.OFF
            AppState.connected -> Screen.CONNECTED
            AppState.everConnected && SystemClock.uptimeMillis() - AppState.lastLossAt < LOST_SHOW_MS -> Screen.LOST
            else -> Screen.WAITING
        }
    }

    private fun refresh() {
        ensureService() // quyền vừa được cấp (vd qua adb) ⇒ khởi động service mà không cần onResume
        val s = computeScreen()
        if (s != screen) { screen = s; showScreen(s) }
        updateDynamic()
    }

    private fun updateDynamic() {
        val pm = getSystemService(Context.POWER_SERVICE) as PowerManager
        setChip(overlayChip, Settings.canDrawOverlays(this), advice = false)
        setChip(batteryChip, pm.isIgnoringBatteryOptimizations(packageName), advice = true)
        codeView?.let { val t = PairingCode.format(PairingStore.code(this)); if (it.text.toString() != t) it.text = t }
        codeChangedView?.visibility = if (AppState.codeChanged) View.VISIBLE else View.GONE
        addrView?.let {
            val ip = NetInfo.ip()
            val err = AppState.serverError
            val t = when {
                err != null -> getString(R.string.wait_server_error, Protocol.PORT.toString(), err)
                ip == null -> getString(R.string.wait_noip)
                else -> getString(R.string.wait_addr, "$ip:${Protocol.PORT}")
            }
            if (it.text.toString() != t) it.text = t
        }
    }

    private fun setChip(v: TextView?, on: Boolean, advice: Boolean) {
        v ?: return
        val t = getString(if (on) R.string.chip_on else if (advice) R.string.chip_off_advice else R.string.chip_off)
        if (v.text.toString() != t) {
            v.text = t
            v.background = pill(if (on) "#1F6B45" else if (advice) "#2A3442" else "#5A4310")
            v.setTextColor(Color.parseColor(if (on) "#D8FFE9" else if (advice) "#C8D3E0" else "#FFD98A"))
        }
    }

    // ------------------------------------------------------------------ dựng màn

    private fun showScreen(s: Screen) {
        holder.removeAllViews()
        hintText = null; codeView = null; addrView = null; codeChangedView = null; overlayChip = null; batteryChip = null; installerBody = null; installerToggle = null
        val row = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL }
        row.setPadding(px(90), px(70), px(70), px(50))
        row.addView(brandColumn())
        val right = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL }
        val focusTarget: View? = when (s) {
            Screen.PERMISSION -> buildPermission(right)
            Screen.WAITING -> buildWaiting(right)
            Screen.CONNECTED -> buildConnected(right)
            Screen.LOST -> buildLost(right)
            Screen.OFF -> buildOff(right)
        }
        val scroll = ScrollView(this).apply { isFillViewport = true; isVerticalScrollBarEnabled = false; addView(right) }
        row.addView(scroll, LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.MATCH_PARENT, 1f).apply { leftMargin = px(60) })
        holder.addView(row, FrameLayout.LayoutParams(-1, -1))
        // focus mặc định CHỈ khi màn mở ra
        focusTarget?.post { focusTarget.requestFocus() }
        applyHint()
    }

    private fun brandColumn(): View {
        val col = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL }
        val word = SpannableString("VietSub").apply { setSpan(ForegroundColorSpan(Color.parseColor("#FFD400")), 4, 7, Spannable.SPAN_EXCLUSIVE_EXCLUSIVE) }
        col.addView(text(word, 76f, bold = true, color = "#FFFFFF"))
        col.addView(text(getString(R.string.brand_lead), 30f, color = "#A9B6C6").apply { setPadding(0, px(18), 0, px(18)) })
        col.addView(text(getString(R.string.brand_hint), 22f, color = "#9AA8B8"))
        col.layoutParams = LinearLayout.LayoutParams(px(560), -2)
        return col
    }

    private fun buildPermission(c: LinearLayout): View {
        c.addView(text(getString(R.string.perm_title), 48f, bold = true))
        c.addView(text(getString(R.string.perm_sub), 28f, color = "#A9B6C6").apply { setPadding(0, px(6), 0, px(22)) })

        val b1 = button(getString(R.string.btn_open_settings)) { requestOverlayPermission() }
        overlayChip = chip()
        c.addView(stepCard("1", getString(R.string.step1_title), getString(R.string.step1_desc), overlayChip!!, b1))

        val b2 = button(getString(R.string.btn_try_settings)) { requestBatteryExemption() }
        batteryChip = chip()
        c.addView(stepCard("2", getString(R.string.step2_title), getString(R.string.step2_desc), batteryChip!!, b2))

        hintText = text("", 22f, color = "#FFE7A8").apply {
            background = rounded("#2A2412", 12, "#6B5418"); setPadding(px(18), px(12), px(18), px(12)); visibility = View.GONE
        }
        c.addView(hintText, LinearLayout.LayoutParams(-1, -2).apply { topMargin = px(8) })
        return b1
    }

    private fun buildWaiting(c: LinearLayout): View {
        c.addView(text(getString(R.string.wait_title), 48f, bold = true))
        c.addView(text(getString(R.string.wait_step), 28f, color = "#A9B6C6").apply { setPadding(0, px(10), 0, px(6)) })
        codeView = text("", 130f, bold = true, color = "#FFFFFF").apply { letterSpacing = 0.06f }
        c.addView(codeView)
        addrView = text("", 30f, color = "#E8EDF2").apply { setPadding(0, px(6), 0, px(6)) }
        c.addView(addrView)
        c.addView(text(getString(R.string.wait_foot), 22f, color = "#9AA8B8"))
        codeChangedView = text(getString(R.string.wait_code_changed), 24f, bold = true, color = "#FFD400").apply {
            setPadding(0, px(14), 0, 0); visibility = View.GONE
        }
        c.addView(codeChangedView)

        val tog = button(getString(R.string.installer_toggle_closed), compact = true) { toggleInstaller() }
        installerToggle = tog
        c.addView(tog, LinearLayout.LayoutParams(-2, -2).apply { topMargin = px(26) })
        installerBody = text(getString(R.string.installer_body), 21f, color = "#CFD8E3").apply {
            background = rounded("#11181F", 12, "#FFD400"); setPadding(px(22), px(16), px(22), px(16)); visibility = View.GONE
        }
        c.addView(installerBody, LinearLayout.LayoutParams(-1, -2).apply { topMargin = px(10) })

        val newCode = button(getString(R.string.btn_new_code), compact = true) {
            // MỘT lần sinh mã: qua service nếu đang chạy (repair() tự sinh), không thì sinh trực tiếp
            val svc = OverlayService.instance
            if (svc != null) svc.repair() else { PairingStore.regenerate(this); AppState.codeChanged = true }
            updateDynamic()
        }
        c.addView(newCode, LinearLayout.LayoutParams(-2, -2).apply { topMargin = px(14) })
        return tog
    }

    private fun toggleInstaller() {
        val b = installerBody ?: return
        val open = b.visibility != View.VISIBLE
        b.visibility = if (open) View.VISIBLE else View.GONE
        installerToggle?.text = getString(if (open) R.string.installer_toggle_open else R.string.installer_toggle_closed)
    }

    private fun buildConnected(c: LinearLayout): View {
        c.addView(text(getString(R.string.conn_title), 48f, bold = true, color = "#7FE0A8"))
        c.addView(text(getString(R.string.conn_sub), 30f, color = "#A9B6C6").apply { setPadding(0, px(8), 0, px(8)) })
        c.addView(text(getString(R.string.conn_overlay_on), 26f, color = "#E8EDF2").apply { setPadding(0, px(4), 0, px(20)) })
        val row = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL }
        val off = button(getString(R.string.btn_overlay_off)) { setOverlay(false) }
        val repair = button(getString(R.string.btn_repair)) { repairNow() }
        row.addView(off); row.addView(repair, LinearLayout.LayoutParams(-2, -2).apply { leftMargin = px(22) })
        c.addView(row)
        c.addView(text(getString(R.string.conn_tip), 22f, color = "#9AA8B8").apply { setPadding(0, px(28), 0, 0) })
        return off
    }

    private fun buildLost(c: LinearLayout): View {
        c.addView(text(getString(R.string.lost_title), 48f, bold = true))
        c.addView(text(getString(R.string.lost_sub), 28f, color = "#A9B6C6").apply { setPadding(0, px(10), 0, px(14)) })
        c.addView(text(getString(R.string.lost_check), 26f, color = "#E8EDF2").apply { setPadding(0, 0, 0, px(24)) })
        val b = button(getString(R.string.btn_repair)) { repairNow() }
        c.addView(b)
        return b
    }

    private fun buildOff(c: LinearLayout): View {
        c.addView(text(getString(R.string.off_title), 48f, bold = true))
        c.addView(text(getString(R.string.off_sub), 28f, color = "#A9B6C6").apply { setPadding(0, px(10), 0, px(6)) })
        c.addView(text(getString(R.string.off_ok), 30f, bold = true, color = "#FFD400").apply { setPadding(0, 0, 0, px(24)) })
        val row = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL }
        val on = button(getString(R.string.btn_overlay_on)) { setOverlay(true) }
        val home = button(getString(R.string.btn_home)) {
            startActivity(Intent(Intent.ACTION_MAIN).addCategory(Intent.CATEGORY_HOME).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
        }
        row.addView(on); row.addView(home, LinearLayout.LayoutParams(-2, -2).apply { leftMargin = px(22) })
        c.addView(row)
        return home   // mockup off:default — focus mặc định "Về màn hình chính" để lỡ bấm OK lần nữa không bật lại
    }

    private fun setOverlay(on: Boolean) {
        OverlayService.instance?.setOverlayEnabled(on) ?: run { AppState.overlayOn = on }
        refresh()
    }

    private fun repairNow() {
        OverlayService.instance?.repair()
        refresh()
    }

    // ------------------------------------------------------------------ quyền (logic đã qua review 2b)

    private fun opensRealScreen(i: Intent): Boolean {
        val ri = packageManager.resolveActivity(i, 0) ?: return true
        return !ri.activityInfo.name.contains("EmptyStub", ignoreCase = true)
    }

    private fun launchSettings(i: Intent, which: Pending, fallbackRes: Int) {
        pending = which
        if (!opensRealScreen(i)) { pending = null; showHint(fallbackRes); return }
        try { startActivity(i) } catch (e: ActivityNotFoundException) { pending = null; showHint(fallbackRes) }
    }

    private fun requestOverlayPermission() {
        showHint(R.string.hint_overlay_list)
        launchSettings(Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:$packageName")), Pending.OVERLAY, R.string.fallback_overlay)
    }

    private fun requestBatteryExemption() {
        launchSettings(Intent(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS, Uri.parse("package:$packageName")), Pending.BATTERY, R.string.fallback_battery)
    }

    /** Sau khi User quay về, nếu việc vừa bấm vẫn chưa xong ⇒ hiện hướng dẫn dự phòng (trạng thái, không theo nút — X15). */
    private fun checkPendingAfterReturn() {
        val p = pending ?: return
        pending = null
        val pm = getSystemService(Context.POWER_SERVICE) as PowerManager
        when (p) {
            Pending.OVERLAY -> if (!Settings.canDrawOverlays(this)) showHint(R.string.fallback_overlay) else showHint(0)
            Pending.BATTERY -> if (!pm.isIgnoringBatteryOptimizations(packageName)) showHint(R.string.fallback_battery) else showHint(0)
        }
    }

    private fun showHint(res: Int) { hintRes = res; applyHint() }

    private fun applyHint() {
        val t = hintText ?: return
        if (hintRes == 0) t.visibility = View.GONE else { t.setText(hintRes); t.visibility = View.VISIBLE }
    }

    // ------------------------------------------------------------------ dựng view

    private fun px(v: Int) = (v * u).toInt()

    private fun text(s: CharSequence, sizeAt1080: Float, bold: Boolean = false, color: String = "#E8EDF2") = TextView(this).apply {
        text = s
        setTextColor(Color.parseColor(color))
        setTextSize(TypedValue.COMPLEX_UNIT_PX, sizeAt1080 * u)
        typeface = if (bold) semibold else regular
        setLineSpacing(0f, 1.15f)
    }

    private fun rounded(fill: String, radius: Int, stroke: String? = null) = GradientDrawable().apply {
        setColor(Color.parseColor(fill)); cornerRadius = px(radius).toFloat()
        if (stroke != null) setStroke(maxOf(1, px(1)), Color.parseColor(stroke))
    }

    private fun pill(fill: String) = GradientDrawable().apply { setColor(Color.parseColor(fill)); cornerRadius = px(40).toFloat() }

    private fun chip() = text("", 20f, bold = true).apply { setPadding(px(14), px(8), px(14), px(8)) }

    private fun stepCard(num: String, title: String, desc: String, chip: TextView, btn: Button): View {
        val card = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            background = rounded("#16202C", 18, "#1AFFFFFF"); setPadding(px(24), px(18), px(24), px(18))
        }
        val head = LinearLayout(this).apply { orientation = LinearLayout.HORIZONTAL; gravity = Gravity.CENTER_VERTICAL }
        val n = text(num, 28f, bold = true).apply { gravity = Gravity.CENTER; background = pill("#243246") }
        head.addView(n, LinearLayout.LayoutParams(px(52), px(52)))
        val tl = LinearLayout(this).apply { orientation = LinearLayout.VERTICAL }
        tl.addView(text(title, 30f, bold = true)); tl.addView(text(desc, 22f, color = "#A9B6C6"))
        head.addView(tl, LinearLayout.LayoutParams(0, -2, 1f).apply { leftMargin = px(20); rightMargin = px(14) })
        head.addView(chip)
        card.addView(head)
        card.addView(btn, LinearLayout.LayoutParams(-2, -2).apply { topMargin = px(14) })
        return card.also { (it.layoutParams as? LinearLayout.LayoutParams)?.bottomMargin = px(14) }.apply {
            layoutParams = LinearLayout.LayoutParams(-1, -2).apply { bottomMargin = px(14) }
        }
    }

    /** B2/X: nút focus nhìn thấy rõ — vàng #FFD400 + chữ đen, phóng 1,05; không focus: nền xám xanh + chữ trắng. */
    private fun button(label: String, compact: Boolean = false, onClick: () -> Unit) = Button(this).apply {
        text = label
        isAllCaps = false
        isFocusable = true
        typeface = semibold
        setTextSize(TypedValue.COMPLEX_UNIT_PX, (if (compact) 24f else 28f) * u)
        minWidth = px(if (compact) 120 else 260); minimumWidth = minWidth
        stateListAnimator = null
        background = StateListDrawable().apply {
            addState(intArrayOf(android.R.attr.state_focused), rounded("#FFD400", 14))
            addState(intArrayOf(android.R.attr.state_pressed), rounded("#FFD400", 14))
            addState(intArrayOf(), rounded("#243246", 14))
        }
        setTextColor(ColorStateList(
            arrayOf(intArrayOf(android.R.attr.state_focused), intArrayOf(android.R.attr.state_pressed), intArrayOf()),
            intArrayOf(Color.parseColor("#111111"), Color.parseColor("#111111"), Color.parseColor("#E8EDF2")),
        ))
        setPadding(px(34), px(18), px(34), px(18))
        setOnClickListener { onClick() }
        setOnFocusChangeListener { v, has ->
            ValueAnimator.ofFloat(v.scaleX, if (has) 1.05f else 1f).apply {
                duration = 120
                addUpdateListener { v.scaleX = it.animatedValue as Float; v.scaleY = it.animatedValue as Float }
                start()
            }
        }
    }

    companion object {
        /** Màn "Mất kết nối" hiện bấy lâu (ms) rồi quay về "Chờ máy tính" (D13: sau ttl). */
        const val LOST_SHOW_MS = 6000L
    }
}
