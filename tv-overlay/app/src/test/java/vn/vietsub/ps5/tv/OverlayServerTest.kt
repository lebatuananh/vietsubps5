package vn.vietsub.ps5.tv

import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.InetAddress
import java.net.Socket
import java.util.concurrent.CopyOnWriteArrayList
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test

class OverlayServerTest {
    private val frames = CopyOnWriteArrayList<OverlayFrame>()
    private val states = CopyOnWriteArrayList<Boolean>()
    private lateinit var server: OverlayServer
    private val sockets = mutableListOf<Socket>()

    @Before fun setUp() {
        server = OverlayServer(0, { "ACDE-FHJK" }, object : OverlayServer.Listener {
            override fun onFrame(frame: OverlayFrame) { frames.add(frame) }
            override fun onConnectionChanged(connected: Boolean) { states.add(connected) }
        })
        server.start()
    }

    @After fun tearDown() { sockets.forEach { runCatching { it.close() } }; server.stop() }

    private fun connect(): Pair<Socket, BufferedReader> {
        val s = Socket(InetAddress.getLoopbackAddress(), server.boundPort)
        s.soTimeout = 3000
        sockets.add(s)
        return s to BufferedReader(InputStreamReader(s.getInputStream(), Charsets.UTF_8))
    }

    private fun send(s: Socket, line: String) { s.getOutputStream().write((line + "\n").toByteArray(Charsets.UTF_8)); s.getOutputStream().flush() }

    private fun hello(token: String = "acdefhjk", v: Int = 1) = """{"hello":{"v":$v,"token":"$token"}}"""

    private fun frame(seq: Long, session: String = "s1") =
        """{"v":1,"sessionId":"$session","seq":$seq,"ttlMs":3000,"items":[]}"""

    private fun await(ms: Long = 3000, cond: () -> Boolean): Boolean {
        val end = System.currentTimeMillis() + ms
        while (System.currentTimeMillis() < end) { if (cond()) return true; Thread.sleep(10) }
        return cond()
    }

    private fun isClosed(r: BufferedReader) = try { r.readLine() == null } catch (_: java.io.IOException) { true }

    @Test fun valid_hello_gets_ok_then_frames_are_delivered() {
        val (s, r) = connect()
        send(s, hello())
        assertEquals("""{"ok":true}""", r.readLine())
        send(s, frame(1))
        assertTrue(await { frames.size == 1 })
        assertEquals(listOf(true), states.toList())
    }

    @Test fun wrong_token_is_rejected_and_closed() {
        val (s, r) = connect()
        send(s, hello("ACDEFHJM"))
        assertEquals("""{"ok":false,"reason":"token"}""", r.readLine())
        assertTrue(isClosed(r))
        assertTrue(frames.isEmpty()); assertTrue(states.isEmpty())
    }

    @Test fun other_version_is_rejected_with_reason_version() {
        val (s, r) = connect()
        send(s, hello(v = 2))
        assertEquals("""{"ok":false,"reason":"version"}""", r.readLine())
        assertTrue(isClosed(r))
    }

    @Test fun first_line_not_hello_closes_without_reply() {
        val (s, r) = connect()
        send(s, frame(1))
        assertTrue(isClosed(r))
        assertTrue(frames.isEmpty())
    }

    @Test fun stale_seq_is_ignored_but_connection_stays() {
        val (s, r) = connect()
        send(s, hello()); r.readLine()
        send(s, frame(5)); send(s, frame(3)); send(s, frame(6))
        assertTrue(await { frames.size == 2 })
        assertEquals(listOf(5L, 6L), frames.map { it.seq })
    }

    @Test fun frame_with_other_version_is_dropped_without_closing() {
        val (s, r) = connect()
        send(s, hello()); r.readLine()
        send(s, """{"v":2,"sessionId":"s","seq":1,"ttlMs":3000,"items":[]}""")
        send(s, frame(1))
        assertTrue(await { frames.size == 1 })
        assertFalse(s.isClosed)
    }

    @Test fun line_over_256KB_closes_the_connection_and_server_keeps_accepting() {
        val (s, r) = connect()
        send(s, hello()); r.readLine()
        s.getOutputStream().write(ByteArray(Protocol.MAX_LINE_BYTES + 100) { 'x'.code.toByte() })
        s.getOutputStream().write('\n'.code)
        s.getOutputStream().flush()
        assertTrue(isClosed(r))
        assertTrue(await { states.lastOrNull() == false })
        val (s2, r2) = connect()
        send(s2, hello())
        assertEquals("""{"ok":true}""", r2.readLine())
    }

    @Test fun new_valid_connection_replaces_the_old_one() {
        val (s1, r1) = connect()
        send(s1, hello()); r1.readLine()
        send(s1, frame(1, "A"))
        assertTrue(await { frames.size == 1 })
        val (s2, r2) = connect()
        send(s2, hello()); assertEquals("""{"ok":true}""", r2.readLine())
        assertTrue(isClosed(r1))                                    // kết nối cũ bị đóng
        send(s2, frame(1, "B"))
        assertTrue(await { frames.size == 2 })
        assertEquals("B", frames.last().sessionId)
    }

    @Test fun invalid_new_connection_does_not_replace_the_old_one() {
        val (s1, r1) = connect()
        send(s1, hello()); r1.readLine()
        val (s2, r2) = connect()
        send(s2, hello("ACDEFHJM")); r2.readLine()
        send(s1, frame(1))
        assertTrue(await { frames.size == 1 })
    }

    @Test fun disconnectCurrent_closes_active_connection_and_reports_false() {
        val (s, r) = connect()
        send(s, hello()); r.readLine()
        assertTrue(await { states.lastOrNull() == true })
        server.disconnectCurrent()
        assertTrue(isClosed(r))
        assertTrue(await { states.lastOrNull() == false })
    }

    @Test fun client_close_reports_disconnected() {
        val (s, r) = connect()
        send(s, hello()); r.readLine()
        s.close()
        assertTrue(await { states.lastOrNull() == false })
    }
}
