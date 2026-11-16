package vn.vietsub.ps5.tv

import java.io.File
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

/**
 * Be Vietnam Pro nhúng phải có đủ dải dấu tiếng Việt U+1EA0–U+1EF9 + chuỗi mẫu của lớp phủ
 * (CLAUDE.md: font thiếu dải ⇒ Android lấy font hệ thống từng chữ, lỗi dễ lọt qua review ảnh).
 * Paint.hasGlyph KHÔNG chứng minh được việc này (có fallback hệ thống) nên kiểm thẳng bảng cmap của file TTF.
 */
class FontCoverageTest {
    private val sample = "HỘP THƯ · CHUYỂN NHƯỢNG — Bản hợp đồng mới đã được gửi tới Ban lãnh đạo. ẾỖẶỮỴ ếỗặữỵ"

    private fun font(name: String) = File("src/main/res/font/$name").readBytes() // cwd của test = thư mục app/

    private fun check(name: String) {
        val cps = FontCoverage.codePoints(font(name))
        val missingRange = (0x1EA0..0x1EF9).filter { it !in cps }
        assertEquals("$name thiếu glyph trong U+1EA0–1EF9: $missingRange", emptyList<Int>(), missingRange)
        val missingSample = sample.codePoints().toArray().filter { !Character.isWhitespace(it) && it !in cps }.distinct()
        assertEquals("$name thiếu glyph chuỗi mẫu: $missingSample", emptyList<Int>(), missingSample)
    }

    @Test fun regularCoversVietnameseRangeAndSample() = check("be_vietnam_pro_regular.ttf")

    @Test fun semiboldCoversVietnameseRangeAndSample() = check("be_vietnam_pro_semibold.ttf")

    @Test
    fun parserDoesNotInventGlyphs_cjkControlIsAbsent() {
        val cps = FontCoverage.codePoints(font("be_vietnam_pro_regular.ttf"))
        assertTrue(0x41 in cps) // 'A' phải có
        assertFalse(0x65E5 in cps) // 日 không có trong Be Vietnam Pro
        assertFalse(0xD55C in cps) // 한 không có
    }
}
