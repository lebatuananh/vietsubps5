package vn.vietsub.ps5.tv

import java.io.ByteArrayInputStream
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class ProtocolTest {
    private fun itemJson(id: String = "a1", text: String = "Ban lãnh đạo", extra: String = "") =
        """{"id":"$id","box":{"x":0.052,"y":0.185,"w":0.048,"h":0.024},"text":"$text","alt":"Ban LĐ","fontPx":22,"group":"col-1","maxLines":1,"align":"start","bold":false,"textColor":"#FFFFFFFF","cover":{"mode":"solid","color":"#FF14202C"},"room":{"x":0.04,"y":0.18,"w":0.2,"h":0.04}$extra}"""

    private fun frameJson(v: Int = 1, session: String = "s1", seq: Long = 1, ttl: Int = 3000, items: String = itemJson(), display: String = ",\"display\":{\"minFontPx\":24,\"holdMs\":150}") =
        "{\"v\":$v,\"sessionId\":\"$session\",\"seq\":$seq,\"ttlMs\":$ttl$display,\"items\":[$items]}"

    // ---- màu
    @Test fun parseColor_accepts_AARRGGBB_any_case() {
        assertEquals(0xFF14202C.toInt(), Protocol.parseColor("#FF14202C"))
        assertEquals(0x80ABCDEF.toInt(), Protocol.parseColor("#80abcdef"))
    }

    @Test fun parseColor_rejects_bad_formats() {
        assertNull(Protocol.parseColor("FF14202C"))
        assertNull(Protocol.parseColor("#14202C"))
        assertNull(Protocol.parseColor("#GG14202C"))
        assertNull(Protocol.parseColor(""))
    }

    // ---- hello
    @Test fun hello_parsed_and_wire_format() {
        val h = Protocol.parseHello("""{"hello":{"v":1,"token":"ACDEFHJK"}}""")!!
        assertEquals(1, h.v); assertEquals("ACDEFHJK", h.token)
        assertNull(Protocol.parseHello("not json"))
        assertNull(Protocol.parseHello("""{"hello":{"v":"x"}}"""))
        assertNull(Protocol.parseHello("""{"v":1}"""))
    }

    @Test fun hello_check_token_case_and_dash_insensitive() {
        assertNull(Protocol.checkHello(Hello(1, "acde-fhjk"), "ACDE-FHJK"))
        assertNull(Protocol.checkHello(Hello(1, "ACDEFHJK"), "acdefhjk"))
    }

    @Test fun hello_check_rejects_wrong_token_and_version() {
        assertEquals("token", Protocol.checkHello(Hello(1, "ACDEFHJM"), "ACDEFHJK"))
        assertEquals("version", Protocol.checkHello(Hello(2, "ACDEFHJK"), "ACDEFHJK"))
    }

    @Test fun replies_exact_wire_format() {
        assertEquals("""{"ok":true}""", Protocol.replyOk())
        assertEquals("""{"ok":false,"reason":"token"}""", Protocol.replyReject("token"))
    }

    // ---- frame
    @Test fun frame_parsed_with_all_fields() {
        val f = Protocol.parseFrame(frameJson())!!
        assertEquals("s1", f.sessionId); assertEquals(1L, f.seq); assertEquals(3000, f.ttlMs)
        assertEquals(24, f.display!!.minFontPx); assertEquals(150, f.display!!.holdMs)
        val it = f.items.single()
        assertEquals("a1", it.id); assertEquals("Ban lãnh đạo", it.text); assertEquals("Ban LĐ", it.alt)
        assertEquals(22, it.fontPx); assertEquals("col-1", it.group); assertEquals(1, it.maxLines)
        assertEquals("start", it.align); assertFalse(it.bold)
        assertEquals(0xFFFFFFFF.toInt(), it.textColor)
        assertEquals("solid", it.cover.mode); assertEquals(0xFF14202C.toInt(), it.cover.color)
        assertEquals(0.052f, it.box.x, 1e-6f); assertEquals(0.2f, it.room!!.w, 1e-6f)
    }

    @Test fun frame_optional_fields_absent_or_null() {
        val bare = """{"v":1,"sessionId":"s","seq":1,"ttlMs":3000,"items":[{"id":"a","box":{"x":0,"y":0,"w":0.1,"h":0.1},"text":"x","fontPx":20,"group":"solo-1","maxLines":1,"align":"center","bold":true,"textColor":"#FF000000","cover":{"mode":"none","color":"#00000000"},"alt":null,"room":null}]}"""
        val f = Protocol.parseFrame(bare)!!
        assertNull(f.display); assertNull(f.items[0].alt); assertNull(f.items[0].room)
    }

    @Test fun frame_empty_items_means_clear() = assertTrue(Protocol.parseFrame(frameJson(items = ""))!!.items.isEmpty())

    @Test fun frame_with_other_version_is_dropped() = assertNull(Protocol.parseFrame(frameJson(v = 2)))

    @Test fun frame_missing_required_fields_is_dropped() {
        assertNull(Protocol.parseFrame("""{"v":1}"""))
        assertNull(Protocol.parseFrame("""{"v":1,"sessionId":"s","seq":1,"ttlMs":3000}"""))
        assertNull(Protocol.parseFrame("garbage"))
    }

    @Test fun frame_unknown_fields_are_ignored() =
        assertNotNull(Protocol.parseFrame(frameJson(items = itemJson(extra = ""","futureField":123"""))))

    @Test fun frame_limits_violations_are_dropped() {
        assertNull(Protocol.parseFrame(frameJson(ttl = 0)))
        assertNull(Protocol.parseFrame(frameJson(ttl = 60_001)))
        assertNotNull(Protocol.parseFrame(frameJson(ttl = 60_000)))
        assertNull(Protocol.parseFrame(frameJson(items = itemJson(text = "x".repeat(1001)))))
        assertNotNull(Protocol.parseFrame(frameJson(items = itemJson(text = "x".repeat(1000)))))
        assertNull(Protocol.parseFrame(frameJson(items = (1..201).joinToString(",") { itemJson(id = "i$it") })))
        assertNotNull(Protocol.parseFrame(frameJson(items = (1..200).joinToString(",") { itemJson(id = "i$it") })))
        assertNull(Protocol.parseFrame(frameJson(items = itemJson().replace("\"maxLines\":1", "\"maxLines\":7"))))
    }

    // ---- seq
    @Test fun seq_stale_and_duplicate_dropped_same_session() {
        val g = SeqGate()
        fun f(s: String, q: Long) = Protocol.parseFrame(frameJson(session = s, seq = q))!!
        assertTrue(g.accept(f("a", 5)))
        assertFalse(g.accept(f("a", 5)))
        assertFalse(g.accept(f("a", 4)))
        assertTrue(g.accept(f("a", 6)))
    }

    @Test fun seq_new_session_resets_counter() {
        val g = SeqGate()
        fun f(s: String, q: Long) = Protocol.parseFrame(frameJson(session = s, seq = q))!!
        assertTrue(g.accept(f("a", 100)))
        assertTrue(g.accept(f("b", 1)))   // phiên mới ⇒ đặt lại
        assertFalse(g.accept(f("b", 1)))
        assertTrue(g.accept(f("a", 1)))   // quay về phiên khác cũng là "phiên mới"
    }

    // ---- đọc dòng
    @Test fun lineReader_reads_lines_and_eof() {
        val r = BoundedLineReader(ByteArrayInputStream("abc\nđây là dòng 2\n".toByteArray()), 100)
        assertEquals("abc", r.readLine()); assertEquals("đây là dòng 2", r.readLine()); assertNull(r.readLine())
    }

    @Test fun lineReader_accepts_exactly_max_and_rejects_longer() {
        val ok = BoundedLineReader(ByteArrayInputStream(("x".repeat(Protocol.MAX_LINE_BYTES) + "\n").toByteArray()))
        assertEquals(Protocol.MAX_LINE_BYTES, ok.readLine()!!.length)
        val bad = BoundedLineReader(ByteArrayInputStream(("x".repeat(Protocol.MAX_LINE_BYTES + 1) + "\n").toByteArray()))
        try { bad.readLine(); throw AssertionError("phải ném LineTooLongException") } catch (_: LineTooLongException) { }
    }

    @Test fun lineReader_rejects_endless_line_without_newline() {
        val bad = BoundedLineReader(ByteArrayInputStream(ByteArray(Protocol.MAX_LINE_BYTES + 50) { 'x'.code.toByte() }))
        try { bad.readLine(); throw AssertionError("phải ném LineTooLongException") } catch (_: LineTooLongException) { }
    }

    @Test fun lineReader_unterminated_tail_is_eof_null() {
        assertNull(BoundedLineReader(ByteArrayInputStream("no newline".toByteArray()), 100).readLine())
    }
}
