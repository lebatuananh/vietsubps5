package vn.vietsub.ps5.tv

import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.InetAddress
import java.net.Socket
import java.net.SocketTimeoutException
import java.util.concurrent.CopyOnWriteArrayList
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class LanFilterTest {
    private fun ok(ip: String) = LanFilter.allowed(InetAddress.getByName(ip))

    @Test fun private_link_local_and_loopback_are_allowed() {
        for (ip in listOf("192.168.1.5", "10.0.0.1", "172.16.5.5", "172.31.255.1", "169.254.1.1", "127.0.0.1", "::1", "fe80::1", "fd12:3456::1"))
            assertTrue(ip, ok(ip))
    }

    @Test fun public_addresses_are_rejected() {
        for (ip in listOf("8.8.8.8", "172.32.0.1", "172.15.0.1", "100.64.0.1", "2001:4860:4860::8888", "2a00:1450::1", "0.0.0.0"))
            assertFalse(ip, ok(ip))
    }
}

class OverlayServerFix3bTest {
    private val frames = CopyOnWriteArrayList<OverlayFrame>()
    private val states = CopyOnWriteArrayList<Boolean>()
    private val sockets = mutableListOf<Socket>()
    private var server: OverlayServer? = null

    private fun start(
        helloTimeoutMs: Long = 5000, maxPending: Int = 4, allowed: (InetAddress) -> Boolean = { true }, idleFactor: Int = 2,
    ): OverlayServer {
        val s = OverlayServer(0, { "ACDE-FHJK" }, object : OverlayServer.Listener {
            override fun onFrame(frame: OverlayFrame) { frames.add(frame) }
            override fun onConnectionChanged(connected: Boolean) { states.add(connected) }
        }, helloTimeoutMs = helloTimeoutMs, maxPending = maxPending, addressAllowed = allowed, idleFactor = idleFactor)
        s.start(); server = s
        return s
    }

    @After fun tearDown() { sockets.forEach { runCatching { it.close() } }; server?.stop() }

    private fun connect(): Pair<Socket, BufferedReader> {
        val s = Socket(InetAddress.getLoopbackAddress(), server!!.boundPort)
        s.soTimeout = 3000
        sockets.add(s)
        return s to BufferedReader(InputStreamReader(s.getInputStream(), Charsets.UTF_8))
    }

    private fun send(s: Socket, line: String) { s.getOutputStream().write((line + "\n").toByteArray(Charsets.UTF_8)); s.getOutputStream().flush() }
    private val hello = """{"hello":{"v":1,"token":"acdefhjk"}}"""
    private fun frame(seq: Long, ttl: Int) = """{"v":1,"sessionId":"s1","seq":$seq,"ttlMs":$ttl,"items":[]}"""

    private fun await(ms: Long = 3000, cond: () -> Boolean): Boolean {
        val end = System.currentTimeMillis() + ms
        while (System.currentTimeMillis() < end) { if (cond()) return true; Thread.sleep(10) }
        return cond()
    }

    private fun isClosed(r: BufferedReader, ms: Int = 1500): Boolean {
        return try { r.readLine() == null } catch (_: SocketTimeoutException) { false } catch (_: java.io.IOException) { true }
    }

    @Test fun source_address_outside_lan_is_dropped_without_any_reply() {
        start(allowed = { false })
        val (s, r) = connect()
        send(s, hello)
        assertTrue(isClosed(r))                                   // đóng, không trả {"ok":…}
        Thread.sleep(150)
        assertTrue(states.isEmpty() && frames.isEmpty())
    }

    @Test fun lan_source_is_accepted() {
        start(allowed = { it.isLoopbackAddress })
        val (s, r) = connect()
        send(s, hello)
        assertEquals("""{"ok":true}""", r.readLine())
    }

    @Test fun hello_has_a_total_deadline_even_when_dribbled_one_byte_at_a_time() {
        start(helloTimeoutMs = 400)
        val (s, r) = connect()
        val t0 = System.currentTimeMillis()
        // nhỏ giọt: mỗi 100 ms một byte, KHÔNG bao giờ hoàn tất dòng
        var closedByServer = false
        try {
            for (i in 1..30) { s.getOutputStream().write('{'.code); s.getOutputStream().flush(); Thread.sleep(100) }
        } catch (_: java.io.IOException) { closedByServer = true }
        val elapsed = System.currentTimeMillis() - t0
        assertTrue("TV phải đóng sau ~400 ms, đã mất $elapsed ms (closed=$closedByServer)", isClosed(r) && elapsed < 2500)
    }

    @Test fun at_most_maxPending_unauthenticated_connections() {
        start(maxPending = 2)
        val silent = (1..4).map { connect() }
        Thread.sleep(300)
        // hai kết nối đầu còn mở (chờ hello), kết nối 3 và 4 bị đóng ngay
        silent[2].first.soTimeout = 500; silent[3].first.soTimeout = 500
        assertTrue(isClosed(silent[2].second)); assertTrue(isClosed(silent[3].second))
        silent[0].first.soTimeout = 300
        assertFalse(isClosed(silent[0].second))
        // giải phóng chỗ chờ ⇒ hello hợp lệ lại được nhận
        silent[0].first.close(); silent[1].first.close()
        Thread.sleep(200)
        val (s, r) = connect()
        send(s, hello)
        assertEquals("""{"ok":true}""", r.readLine())
    }

    @Test fun half_open_connection_is_closed_after_2x_ttl_and_reports_lost() {
        start()
        val (s, r) = connect()
        send(s, hello)
        assertEquals("""{"ok":true}""", r.readLine())
        send(s, frame(1, ttl = 100))                              // rồi im lặng, KHÔNG đóng socket (nửa mở)
        assertTrue(await { states == listOf(true) })
        assertTrue("phải về 'mất kết nối' trong ~2×ttl", await(2500) { states == listOf(true, false) })
    }

    @Test fun heartbeats_within_2x_ttl_keep_the_connection() {
        start()
        val (s, r) = connect()
        send(s, hello)
        assertEquals("""{"ok":true}""", r.readLine())
        for (i in 1L..8L) { send(s, frame(i, ttl = 300)); Thread.sleep(150) }   // 1,2 s > 2×ttl nếu không có nhịp
        assertEquals(listOf(true), states.toList())
        assertEquals(8, frames.size)
    }
}
