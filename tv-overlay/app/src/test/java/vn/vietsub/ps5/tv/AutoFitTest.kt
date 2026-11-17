package vn.vietsub.ps5.tv

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNotEquals
import org.junit.Assert.assertTrue
import org.junit.Test

private class FakeMeasurer : TextMeasurer {
    override fun width(text: String, sizePx: Float, bold: Boolean) = text.length * sizePx * 0.5f
    override fun lineHeight(sizePx: Float, bold: Boolean) = sizePx * 1.2f
}

private fun item(
    id: String, text: String, fontPx: Int = 30, group: String = "solo-$id", maxLines: Int = 1, alt: String? = null,
    align: String = "start", room: Box? = null,
) = OverlayItem(id, Box(0f, 0f, 0.1f, 0.03f), text, alt, fontPx, group, maxLines, align, false, 0xFFFFFFFF.toInt(),
    CoverSpec("solid", 0xFF14202C.toInt()), room)

private fun input(it: OverlayItem, l: Float, t: Float, w: Float, h: Float, room: Rect? = null) = FitInput(it, Rect(l, t, l + w, t + h), room)

class AutoFitTest {

    // B4 (review 3a): engine thường gửi fontPx < display.minFontPx (vd 18 < 24). TV bắt đầu từ max(fontPx×H/1080, minFontPx).
    @Test fun fontPx_below_min_font_starts_at_min_not_at_fontPx() {
        val r = fit.fit(listOf(input(item("a", "Hộp thư", fontPx = 18), 100f, 100f, 200f, 32f))).single()
        assertEquals(24f, r.sizePx, 0.01f)
        assertEquals(listOf("Hộp thư"), r.lines)
    }

    // ===== Rev 1.4 (review 3b B1): thứ tự co cỡ → alt → thêm dòng (trong chiều cao room) → sàn 0,85×min → "…" =====
    private val room283x63 = Rect(90f, 90f, 373f, 153f)          // room 283×63 quanh box 189×35 ở (100,100)

    @Test fun narrow_row_label_shrinks_below_min_to_floor_and_stays_on_one_line() {
        // "Cập nhật chấn thương"-kiểu: 24 ký tự; ở min 24 rộng 288 > room 273 ⇒ KHÔNG xuống dòng, co tới sàn 20,4 (rộng 245)
        val r = fit.fit(listOf(input(item("a", "x".repeat(24), fontPx = 30, maxLines = 1), 100f, 100f, 189f, 35f, room283x63))).single()
        assertEquals(1, r.lines.size)
        assertTrue("cỡ ${r.sizePx}", r.sizePx < 24f && r.sizePx >= 24f * AutoFit.FLOOR_FACTOR - 0.01f)
        assertTrue(r.textRect.r <= room283x63.r + 0.01f && r.textRect.b <= room283x63.b + 0.01f)
        assertFalse(r.lines.single().endsWith("…"))
    }

    @Test fun second_label_case_ban_lanh_dao_fits_one_line_in_box_width() {
        val r = fit.fit(listOf(input(item("b", "Ban lãnh đạo", fontPx = 30, maxLines = 1), 100f, 100f, 189f, 35f, room283x63))).single()
        assertEquals(listOf("Ban lãnh đạo"), r.lines)
        assertTrue(r.sizePx >= 24f)
    }

    @Test fun label_that_cannot_fit_even_at_floor_is_ellipsized_last_and_reported() {
        val log = mutableListOf<String>()
        val af = AutoFit(FakeMeasurer(), 1920f, 1080f, 24) { log.add(it) }
        val r = af.fit(listOf(input(item("a", "x".repeat(60), fontPx = 30, maxLines = 1), 100f, 100f, 189f, 35f, room283x63))).single()
        assertEquals(1, r.lines.size)
        assertTrue(r.lines.single().endsWith("…"))
        assertEquals(24f * AutoFit.FLOOR_FACTOR, r.sizePx, 0.01f)
        assertTrue(r.textRect.r <= room283x63.r + 0.01f)
        assertTrue(log.isNotEmpty())
    }

    @Test fun label_uses_alt_before_ellipsis() {
        val r = fit.fit(listOf(input(item("a", "x".repeat(60), fontPx = 30, maxLines = 1, alt = "Ngắn"), 100f, 100f, 189f, 35f, room283x63))).single()
        assertTrue(r.usedAlt); assertEquals(listOf("Ngắn"), r.lines)
    }

    @Test fun extra_lines_are_added_only_within_room_height() {
        // đoạn văn (maxLines 3 = 1 dòng gốc + 2): 2 dòng ở cỡ 30 cao 72 > room 60 ⇒ phải co tới 24 (57,6) thay vì tràn đáy room
        val room = Rect(90f, 90f, 400f, 150f)                    // cao 60
        val txt = "tu".repeat(20)                               // 40 ký tự
        val r = fit.fit(listOf(input(item("p", txt, fontPx = 30, maxLines = 3), 100f, 100f, 300f, 32f, room))).single()
        assertTrue("textRect.b=${r.textRect.b} > room.b", r.textRect.b <= room.b + 0.01f)
        assertTrue(r.textRect.t >= room.t - 0.01f)
        assertTrue(r.lines.size <= 2)
    }

    @Test fun legacy_label_with_extra_lines_may_wrap_only_inside_room_height() {
        val roomTall = Rect(90f, 90f, 373f, 153f)
        val r = fit.fit(listOf(input(item("a", "x".repeat(30), fontPx = 30, maxLines = 3), 100f, 100f, 189f, 35f, roomTall))).single()
        assertTrue(r.textRect.b <= roomTall.b + 0.01f)
    }

    @Test fun paragraph_shrinks_to_min_before_adding_a_line() {
        // maxLines 4 = 2 dòng gốc + 2; chữ vừa 2 dòng ở cỡ 24 nhưng cần 3 dòng ở cỡ 30 ⇒ chọn cỡ nhỏ hơn với 2 dòng
        val words = (1..12).joinToString(" ") { "abcd" }            // 59 ký tự: cỡ 30 cần 3 dòng, cỡ ≤ 26 chỉ cần 2
        val r = fit.fit(listOf(input(item("p", words, fontPx = 30, maxLines = 4), 100f, 100f, 400f, 120f))).single()
        assertTrue(r.lines.size <= 2)
        assertTrue(r.sizePx < 30f && r.sizePx >= 24f)
    }

    @Test fun fontPx_below_min_font_scales_min_with_plane_height() {
        val fit2 = AutoFit(FakeMeasurer(), 3840f, 2160f, 24)      // 4K: min = 24 × 2
        val r = fit2.fit(listOf(input(item("a", "Hộp thư", fontPx = 18), 100f, 100f, 400f, 64f))).single()
        assertEquals(48f, r.sizePx, 0.01f)
    }
    private val fit = AutoFit(FakeMeasurer(), 1920f, 1080f, 24)

    @Test fun short_label_keeps_requested_size_one_line_inside_box() {
        val r = fit.fit(listOf(input(item("a", "Hộp thư"), 100f, 100f, 200f, 32f))).single()
        assertEquals(30f, r.sizePx, 0.01f)
        assertEquals(listOf("Hộp thư"), r.lines)
        assertFalse(r.usedAlt)
        assertTrue(r.textRect.r <= 300f + 0.01f)
    }

    @Test fun label_shrinks_until_it_fits_the_box_width() {
        // 20 ký tự: rộng = 20*size*0.5 ≤ 250 ⇒ size ≤ 25
        val r = fit.fit(listOf(input(item("a", "x".repeat(20)), 100f, 100f, 250f, 32f))).single()
        assertEquals(25f, r.sizePx, 0.01f)
        assertTrue(r.textRect.w <= 250.01f)
    }

    @Test fun label_never_wraps_never_goes_below_floor_and_ellipsizes_only_as_last_resort() {
        // Rev 1.4: sàn = 0,85 × min; không vừa một dòng kể cả ở sàn ⇒ cắt "…" (không bao giờ bẻ dòng)
        val txt = "y".repeat(60)
        val r = fit.fit(listOf(input(item("a", txt), 100f, 100f, 100f, 32f))).single()
        assertEquals(24f * AutoFit.FLOOR_FACTOR, r.sizePx, 0.01f)
        assertEquals(1, r.lines.size)
        assertTrue(r.lines.single().endsWith("…"))
    }

    @Test fun paragraph_wraps_within_box_and_respects_maxLines() {
        val words = (1..14).joinToString(" ") { "tu$it" }            // ~ 60 ký tự
        val r = fit.fit(listOf(input(item("p", words, 30, maxLines = 5), 100f, 100f, 300f, 80f))).single()
        assertTrue(r.lines.size in 2..5)
        assertTrue(r.sizePx >= 24f)
        r.lines.forEach { assertTrue(it, FakeMeasurer().width(it, r.sizePx, false) <= 300.01f) }
        assertFalse(r.lines.any { it.endsWith("…") })
    }

    @Test fun paragraph_that_cannot_fit_is_truncated_with_ellipsis_only_on_last_line() {
        val words = (1..80).joinToString(" ") { "tu$it" }
        val r = fit.fit(listOf(input(item("p", words, 30, maxLines = 3), 100f, 100f, 300f, 80f))).single()
        assertEquals(3, r.lines.size)
        assertEquals(24f * AutoFit.FLOOR_FACTOR, r.sizePx, 0.01f)       // sàn 0,85 × min trước khi cắt "…"
        assertTrue(r.lines.last().endsWith("…"))
        assertFalse(r.lines.dropLast(1).any { it.endsWith("…") })
        r.lines.forEach { assertTrue(FakeMeasurer().width(it, r.sizePx, false) <= 300.01f) }
    }

    @Test fun wraps_and_expands_only_inside_room() {
        val room = Rect(90f, 90f, 490f, 150f)                        // rộng 400
        val r = fit.fit(listOf(input(item("a", "z".repeat(28), 30), 100f, 100f, 100f, 32f, room))).single()
        assertTrue(r.textRect.r <= room.r + 0.01f)
        assertTrue(r.textRect.l >= room.l - 0.01f)
        assertTrue(r.textRect.b <= room.b + 0.01f)
        assertTrue(r.textRect.w > 100f)                              // đã nới ra ngoài box (nhưng trong room)
    }

    @Test fun label_that_may_wrap_prefers_one_line_in_room_over_wrapping_in_the_narrow_box() {
        // "Ban lãnh đạo" (12 ký tự) ở cỡ 24 rộng 144; box chỉ rộng 92 nhưng room rộng 300 ⇒ 1 dòng trong room, không bẻ 3 dòng
        val room = Rect(90f, 90f, 390f, 140f)
        val r = fit.fit(listOf(input(item("a", "Ban lãnh đạo", fontPx = 20, maxLines = 3), 100f, 100f, 92f, 32f, room))).single()
        assertEquals(1, r.lines.size)
        assertTrue(r.textRect.r <= room.r + 0.01f)
    }

    @Test fun same_group_gets_same_size_equal_to_the_minimum() {
        val a = item("a", "x".repeat(20), group = "col-1")           // cần 25
        val b = item("b", "ok", group = "col-1")                     // vừa ở 30
        val rs = fit.fit(listOf(input(a, 100f, 100f, 250f, 32f), input(b, 100f, 200f, 250f, 32f)))
        assertEquals(25f, rs[0].sizePx, 0.01f)
        assertEquals(25f, rs[1].sizePx, 0.01f)
    }

    @Test fun different_groups_are_independent() {
        val a = item("a", "x".repeat(20), group = "col-1")
        val b = item("b", "ok", group = "col-2")
        val rs = fit.fit(listOf(input(a, 100f, 100f, 250f, 32f), input(b, 500f, 100f, 250f, 32f)))
        assertEquals(25f, rs[0].sizePx, 0.01f)
        assertEquals(30f, rs[1].sizePx, 0.01f)
    }

    private fun rowItems(altFirst: String?) = listOf(
        input(item("t1", "x".repeat(13), group = "row-1", alt = altFirst), 100f, 50f, 200f, 32f),
        input(item("t2", "y".repeat(8), group = "row-1"), 300f, 50f, 200f, 32f),
        input(item("t3", "z".repeat(6), group = "row-1"), 500f, 50f, 200f, 32f),
    )

    @Test fun row_label_lacking_gap_uses_alt_only_for_that_label() {
        // t1 ở cỡ 30 rộng 195 ⇒ khe tới t2 = 5 px < 2% × 1920 = 38,4 ⇒ dùng alt "CN"
        val rs = fit.fit(rowItems("CN"))
        assertTrue(rs[0].usedAlt); assertEquals(listOf("CN"), rs[0].lines)
        assertFalse(rs[1].usedAlt); assertFalse(rs[2].usedAlt)
        assertEquals(1, rs.map { it.sizePx }.distinct().size)       // cùng nhóm cùng cỡ
    }

    @Test fun row_without_alt_shrinks_the_whole_group_until_gaps_ok() {
        val rs = fit.fit(rowItems(null))
        assertEquals(1, rs.map { it.sizePx }.distinct().size)
        val s = rs[0].sizePx
        assertTrue(s < 30f)
        assertTrue(s >= 24f)
        assertFalse(rs.any { it.usedAlt })
        val gap = 300f - (100f + rs[0].textRect.w)
        assertTrue("khe=$gap", gap >= 0.02f * 1920f - 0.01f)
    }

    @Test fun alt_is_not_used_when_there_is_enough_gap() {
        val roomy = listOf(
            input(item("t1", "x".repeat(6), group = "row-1", alt = "A"), 100f, 50f, 80f, 32f),
            input(item("t2", "y".repeat(6), group = "row-1", alt = "B"), 400f, 50f, 80f, 32f),
        )
        assertFalse(fit.fit(roomy).any { it.usedAlt })
    }

    @Test fun fontPx_and_min_scale_with_plane_height() {
        val big = AutoFit(FakeMeasurer(), 3840f, 2160f, 24)
        val r = big.fit(listOf(input(item("a", "ok", 30), 100f, 100f, 800f, 64f))).single()
        assertEquals(60f, r.sizePx, 0.01f)
        val tight = big.fit(listOf(input(item("a", "q".repeat(400), 30), 100f, 100f, 100f, 64f))).single()
        assertEquals(48f * AutoFit.FLOOR_FACTOR, tight.sizePx, 0.01f)   // sàn = 0,85 × (min 24 × 2)
    }

    @Test fun align_center_and_end_position_text_inside_box() {
        val c = fit.fit(listOf(input(item("c", "abcd", align = "center"), 100f, 100f, 200f, 32f))).single()
        assertEquals(200f, (c.textRect.l + c.textRect.r) / 2f, 0.5f)
        val e = fit.fit(listOf(input(item("e", "abcd", align = "end"), 100f, 100f, 200f, 32f))).single()
        assertEquals(300f, e.textRect.r, 0.5f)
        assertNotEquals(c.textRect.l, e.textRect.l)
    }
}

class CoverRegionTest {
    private val plane = Rect(0f, 0f, 1920f, 1080f)

    @Test fun union_of_box_and_text_plus_margin() {
        val box = Rect(100f, 100f, 300f, 130f)
        val text = Rect(100f, 100f, 360f, 140f)
        val c = CoverRegion.compute(box, text, 6f, null, plane)
        assertTrue(c.l <= 100f - 6f && c.t <= 100f - 6f && c.r >= 360f + 6f && c.b >= 140f + 6f)
        assertTrue(c.l <= box.l && c.r >= box.r && c.t <= box.t && c.b >= box.b)
    }

    @Test fun min_margin_is_6_of_1080_of_plane_height() {
        assertEquals(6f, CoverRegion.minMargin(1080f), 1e-4f)
        assertEquals(12f, CoverRegion.minMargin(2160f), 1e-4f)
        // lề thực tế quanh bbox gốc ≥ lề tối thiểu
        val box = Rect(500f, 500f, 700f, 530f)
        val c = CoverRegion.compute(box, Rect(500f, 500f, 600f, 520f), CoverRegion.minMargin(1080f), null, plane)
        assertTrue(box.l - c.l >= 6f - 1e-3f && c.r - box.r >= 6f - 1e-3f && box.t - c.t >= 6f - 1e-3f && c.b - box.b >= 6f - 1e-3f)
    }

    @Test fun never_outside_room() {
        val box = Rect(100f, 100f, 300f, 130f)
        val text = Rect(100f, 100f, 450f, 130f)
        val room = Rect(95f, 95f, 460f, 140f)
        val c = CoverRegion.compute(box, text, 12f, room, plane)
        assertTrue(c.l >= room.l && c.t >= room.t && c.r <= room.r && c.b <= room.b)
        assertTrue(c.l <= box.l && c.r >= box.r && c.t <= box.t && c.b >= box.b)   // vẫn phủ bbox gốc
    }

    @Test fun never_outside_plane() {
        val c = CoverRegion.compute(Rect(0f, 0f, 50f, 20f), Rect(0f, 0f, 80f, 20f), 10f, null, plane)
        assertTrue(c.l >= 0f && c.t >= 0f)
        val d = CoverRegion.compute(Rect(1880f, 1060f, 1920f, 1080f), Rect(1850f, 1060f, 1920f, 1080f), 10f, null, plane)
        assertTrue(d.r <= 1920f && d.b <= 1080f)
    }

    @Test fun card_is_fully_opaque() {
        assertEquals(0xFF112233.toInt(), CoverRegion.opaque(0x80112233.toInt()))
        assertEquals(0xFFFFFFFF.toInt(), CoverRegion.opaque(0x00FFFFFF))
        assertEquals(0xFF000000.toInt(), CoverRegion.opaque(0xCC000000.toInt()))
    }
}
