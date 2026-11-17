package vn.vietsub.ps5.tv

import android.content.Context
import java.net.Inet4Address
import java.net.NetworkInterface

/** Trạng thái dùng chung giữa OverlayService (ghi) và MainActivity (đọc, thăm dò). */
object AppState {
    @Volatile var running = false
    @Volatile var connected = false
    @Volatile var everConnected = false
    @Volatile var lastLossAt = 0L
    @Volatile var overlayOn = true
    @Volatile var serverError: String? = null
    /** MainActivity đang ở tiền cảnh ⇒ lớp phủ ẩn (chữ engine không đè giao diện app). */
    @Volatile var appInForeground = false
    /** Mã vừa đổi (Ghép cặp lại / Tạo mã mới) và chưa có engine nào nối lại ⇒ màn Chờ nhắc chạy lại `engine pair`. */
    @Volatile var codeChanged = false
}

/** Mã ghép cặp: sinh MỘT lần, lưu SharedPreferences; "Ghép cặp lại" sinh mã mới. */
object PairingStore {
    private const val PREFS = "pairing"
    private const val KEY = "code"

    @Synchronized
    fun code(ctx: Context): String {
        val p = ctx.applicationContext.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        val cur = p.getString(KEY, null)
        if (cur != null && PairingCode.isValid(cur)) return PairingCode.normalize(cur)
        return regenerate(ctx)
    }

    @Synchronized
    fun regenerate(ctx: Context): String {
        val c = PairingCode.generate()
        ctx.applicationContext.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putString(KEY, c).apply()
        return c
    }
}

object NetInfo {
    /** IPv4 LAN của TV (site-local); null nếu chưa có mạng. */
    fun ip(): String? = try {
        NetworkInterface.getNetworkInterfaces().toList().filter { it.isUp && !it.isLoopback }
            .flatMap { it.inetAddresses.toList() }
            .firstOrNull { it is Inet4Address && it.isSiteLocalAddress }?.hostAddress
    } catch (e: Exception) {
        null
    }
}
