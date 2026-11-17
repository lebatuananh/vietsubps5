package vn.vietsub.ps5.tv

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Test

class OverlayModelTest {
    private fun it(id: String, text: String = "Xin chào") = OverlayItem(id, Box(0.1f, 0.1f, 0.1f, 0.03f), text, null, 24, "solo-$id", 1, "start", false,
        0xFFFFFFFF.toInt(), CoverSpec("solid", 0xFF000000.toInt()), null)

    private fun frame(seq: Long, vararg items: OverlayItem, ttl: Int = 3000, hold: Int? = 0, session: String = "s") =
        OverlayFrame(session, seq, ttl, hold?.let { Display(24, it) }, items.toList())

    private fun alphaOf(m: OverlayModel, id: String, now: Long) = m.snapshot(now).firstOrNull { it.item.id == id }?.alpha

    @Test fun new_item_fades_in_over_120ms() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a")), 1000)
        assertEquals(0f, alphaOf(m, "a", 1000)!!, 1e-3f)
        assertEquals(0.5f, alphaOf(m, "a", 1060)!!, 1e-3f)
        assertEquals(1f, alphaOf(m, "a", 1120)!!, 1e-3f)
        assertEquals(1f, alphaOf(m, "a", 4000)!!, 1e-3f)           // đúng hạn ttl (1000+3000) vẫn 1
    }

    @Test fun same_id_same_content_is_not_redrawn_or_refaded() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a")), 0)
        val v = m.version
        m.applyFrame(frame(2, it("a")), 500)
        assertEquals(v, m.version)
        assertEquals(1f, alphaOf(m, "a", 500)!!, 1e-3f)
    }

    @Test fun same_id_changed_content_updates_without_fade() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a", "Một")), 0)
        val v = m.version
        m.applyFrame(frame(2, it("a", "Hai")), 500)
        assertTrue(m.version > v)
        val s = m.snapshot(500).single()
        assertEquals("Hai", s.item.text); assertEquals(1f, s.alpha, 1e-3f)
    }

    @Test fun empty_frame_with_hold_keeps_old_layer_then_fades_out_80ms() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a"), hold = 300), 0)
        m.applyFrame(frame(2, hold = 300), 500)
        assertEquals(1f, alphaOf(m, "a", 700)!!, 1e-3f)
        assertEquals(1f, alphaOf(m, "a", 800)!!, 1e-3f)
        assertEquals(0.5f, alphaOf(m, "a", 840)!!, 1e-3f)
        assertNull(alphaOf(m, "a", 880))
        assertTrue(m.snapshot(900).isEmpty())
    }

    @Test fun empty_frame_without_hold_fades_out_immediately() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a")), 0)
        m.applyFrame(frame(2), 500)
        assertEquals(0.5f, alphaOf(m, "a", 540)!!, 1e-3f)
        assertNull(alphaOf(m, "a", 580))
    }

    @Test fun missing_display_reuses_last_known_hold() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a"), hold = 200), 0)
        m.applyFrame(frame(2, hold = null), 500)
        assertEquals(1f, alphaOf(m, "a", 650)!!, 1e-3f)
        assertEquals(200, m.display().holdMs)
    }

    @Test fun single_item_disappearing_fades_out_while_others_stay() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a"), it("b")), 0)
        m.applyFrame(frame(2, it("a")), 500)
        assertNull(alphaOf(m, "b", 600))
        assertEquals(1f, alphaOf(m, "a", 600)!!, 1e-3f)
    }

    @Test fun ttl_expiry_clears_the_layer() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a"), ttl = 3000), 0)
        assertEquals(1f, alphaOf(m, "a", 3000)!!, 1e-3f)
        assertEquals(0.5f, alphaOf(m, "a", 3040)!!, 1e-3f)
        assertTrue(m.snapshot(3100).isEmpty())
    }

    @Test fun new_frame_after_expiry_shows_again() {
        val m = OverlayModel()
        m.applyFrame(frame(1, it("a"), ttl = 1000), 0)
        assertTrue(m.snapshot(2000).isEmpty())
        m.applyFrame(frame(2, it("a")), 5000)
        assertEquals(0f, alphaOf(m, "a", 5000)!!, 1e-3f)
        assertEquals(1f, alphaOf(m, "a", 5200)!!, 1e-3f)
    }

    @Test fun nextEvent_and_animating_flags() {
        val m = OverlayModel()
        assertFalse(m.isAnimating(0)); assertNull(m.nextEventMs(0))
        m.applyFrame(frame(1, it("a"), ttl = 3000), 0)
        assertTrue(m.isAnimating(50))                               // đang fade-in
        assertFalse(m.isAnimating(1000))                            // ổn định
        assertEquals(3000L, m.nextEventMs(1000))                    // hạn ttl
    }

    // Rev 1.4 (NB4 review 3b): engine đảm bảo id duy nhất; nếu vẫn trùng, TV GIỮ item đầu, bỏ phần còn lại và LOG.
    @Test fun duplicate_ids_in_one_frame_keep_the_first_and_log() {
        val warns = mutableListOf<String>()
        val m = OverlayModel { warns.add(it) }
        val a = it("dup", "Ban lãnh đạo")
        val b = it("dup", "Hôm nay").copy(box = Box(0.1f, 0.2f, 0.1f, 0.03f))
        m.applyFrame(frame(1, a, b), 0)
        val vis = m.snapshot(500)
        assertEquals(1, vis.size)
        assertEquals("Ban lãnh đạo", vis[0].item.text)
        assertTrue(warns.isNotEmpty())
        val v = m.version
        m.applyFrame(frame(2, a, b), 700)                         // khung sau y hệt ⇒ không vẽ lại
        assertEquals(v, m.version)
    }
}
