package vn.vietsub.ps5.tv

import java.io.IOException
import java.net.Inet4Address
import java.net.InetAddress
import java.net.InetSocketAddress
import java.net.ServerSocket
import java.net.Socket
import java.util.Timer
import java.util.TimerTask
import java.util.concurrent.atomic.AtomicBoolean
import java.util.concurrent.atomic.AtomicInteger

/**
 * Server TCP của TV (JVM thuần, test được bằng loopback): MỘT kết nối hoạt động; kết nối mới HỢP LỆ thay kết nối cũ.
 * hello sai ⇒ trả `{"ok":false,"reason":…}` rồi đóng; dòng > 256 KB ⇒ đóng.
 * Rev 1.4: chỉ nhận nguồn LAN riêng ([LanFilter]); hello phải HOÀN TẤT trong [helloTimeoutMs] (đóng cưỡng bức, kể cả nhỏ giọt);
 * tối đa [maxPending] kết nối chưa xác thực; kết nối nửa mở: không nhận gói nào trong [idleFactor] × `ttlMs` ⇒ đóng.
 */
class OverlayServer(
    private val port: Int,
    private val tokenProvider: () -> String,
    private val listener: Listener,
    private val log: (String) -> Unit = {},
    private val helloTimeoutMs: Long = 5000,
    private val maxPending: Int = 4,
    private val addressAllowed: (java.net.InetAddress) -> Boolean = LanFilter::allowed,
    private val idleFactor: Int = 2,
) {
    interface Listener {
        /** Gọi trên luồng nền của server. */
        fun onFrame(frame: OverlayFrame)

        fun onConnectionChanged(connected: Boolean)
    }

    private var server: ServerSocket? = null
    private val lock = Any()
    private var current: Socket? = null
    private val gate = SeqGate()
    @Volatile private var running = false
    private val pending = AtomicInteger(0)
    private var watchdog: Timer? = null

    /** Cổng thực sự đang nghe (port = 0 ⇒ cổng tạm, dùng cho test). */
    val boundPort: Int get() = server?.localPort ?: -1

    fun start() {
        if (running) return
        val ss = ServerSocket()
        ss.reuseAddress = true
        ss.bind(InetSocketAddress(port))
        server = ss
        running = true
        watchdog = Timer("vietsub-hello-watchdog", true)
        Thread({ acceptLoop(ss) }, "vietsub-accept").apply { isDaemon = true; start() }
    }

    fun stop() {
        running = false
        runCatching { server?.close() }
        runCatching { watchdog?.cancel() }
        disconnectCurrent()
    }

    /** Ngắt kết nối hiện tại (ghép cặp lại). */
    fun disconnectCurrent() {
        val s = synchronized(lock) { current }
        runCatching { s?.close() }
    }

    private companion object { const val DEFAULT_TTL_MS = 3000 }

    private fun acceptLoop(ss: ServerSocket) {
        while (running) {
            val s = try { ss.accept() } catch (e: IOException) { break }
            if (!addressAllowed(s.inetAddress)) {                       // NB2: không phải mạng riêng ⇒ đóng, không trả lời
                log("[server] từ chối nguồn ngoài LAN: ${s.inetAddress?.hostAddress}")
                runCatching { s.close() }
                continue
            }
            if (pending.incrementAndGet() > maxPending) {               // NB3: quá nhiều kết nối chưa xác thực
                pending.decrementAndGet()
                log("[server] quá $maxPending kết nối chưa xác thực — đóng kết nối mới")
                runCatching { s.close() }
                continue
            }
            Thread({ handle(s) }, "vietsub-conn").apply { isDaemon = true; start() }
        }
    }

    private fun handle(s: Socket) {
        var active = false
        val authed = AtomicBoolean(false)
        val pendingHeld = AtomicBoolean(true)
        fun releasePending() { if (pendingHeld.compareAndSet(true, false)) pending.decrementAndGet() }
        // Hạn hello TỔNG (không phải mỗi lần đọc): quá hạn mà chưa xác thực ⇒ đóng cưỡng bức socket.
        val deadline = object : TimerTask() {
            override fun run() { if (!authed.get()) { log("[server] hello quá hạn ${helloTimeoutMs} ms — đóng"); runCatching { s.close() } } }
        }
        try {
            runCatching { watchdog?.schedule(deadline, helloTimeoutMs) }
            s.tcpNoDelay = true
            s.soTimeout = 5000
            val reader = BoundedLineReader(s.getInputStream())
            val out = s.getOutputStream()
            val hello = Protocol.parseHello(reader.readLine() ?: return) ?: return
            val reason = Protocol.checkHello(hello, tokenProvider())
            if (reason != null) {
                out.write((Protocol.replyReject(reason) + "\n").toByteArray()); out.flush()
                log("[server] từ chối hello: $reason")
                return
            }
            val old: Socket?
            synchronized(lock) { old = current; current = s }
            active = true
            authed.set(true); deadline.cancel(); releasePending()
            runCatching { old?.close() }                      // kết nối mới hợp lệ thay kết nối cũ
            out.write((Protocol.replyOk() + "\n").toByteArray()); out.flush()
            s.soTimeout = idleFactor * DEFAULT_TTL_MS         // NB5: nửa mở ⇒ hết hạn đọc ⇒ đóng ⇒ "mất kết nối"
            if (old == null) listener.onConnectionChanged(true)
            while (true) {
                val line = reader.readLine() ?: break
                val frame = Protocol.parseFrame(line) ?: continue
                s.soTimeout = idleFactor * frame.ttlMs.coerceAtLeast(1)
                if (!gate.accept(frame)) continue
                listener.onFrame(frame)
            }
        } catch (e: LineTooLongException) {
            log("[server] đóng kết nối: ${e.message}")
        } catch (e: IOException) {
            // đóng/ timeout — kết thúc êm
        } finally {
            deadline.cancel(); releasePending()
            var wasCurrent = false
            synchronized(lock) { if (current === s) { current = null; wasCurrent = true } }
            runCatching { s.close() }
            if (active && wasCurrent) listener.onConnectionChanged(false)
        }
    }
}

/** Chỉ nhận kết nối từ mạng riêng (RFC 1918, link-local, loopback, IPv6 ULA/link-local). */
object LanFilter {
    fun allowed(a: InetAddress): Boolean {
        if (a.isLoopbackAddress) return true
        if (a is Inet4Address) return a.isSiteLocalAddress || a.isLinkLocalAddress   // 10/8, 172.16/12, 192.168/16, 169.254/16
        if (a.isLinkLocalAddress) return true                                        // fe80::/10
        return (a.address[0].toInt() and 0xFE) == 0xFC                               // fc00::/7 (ULA)
    }
}
