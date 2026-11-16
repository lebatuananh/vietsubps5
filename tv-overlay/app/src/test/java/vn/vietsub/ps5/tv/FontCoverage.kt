package vn.vietsub.ps5.tv

import java.nio.ByteBuffer

/** Đọc bảng cmap của file TTF (format 4 và 12) — kiểm font có glyph thật, không qua fallback hệ thống. */
object FontCoverage {
    fun codePoints(ttf: ByteArray): Set<Int> {
        val b = ByteBuffer.wrap(ttf) // big-endian mặc định
        val numTables = b.getShort(4).toInt() and 0xFFFF
        var cmapOffset = -1
        for (i in 0 until numTables) {
            val rec = 12 + i * 16
            val tag = String(ttf, rec, 4, Charsets.US_ASCII)
            if (tag == "cmap") cmapOffset = b.getInt(rec + 8)
        }
        require(cmapOffset >= 0) { "Không có bảng cmap" }

        val result = HashSet<Int>()
        val subtables = b.getShort(cmapOffset + 2).toInt() and 0xFFFF
        for (i in 0 until subtables) {
            val sub = cmapOffset + b.getInt(cmapOffset + 4 + i * 8 + 4)
            when (b.getShort(sub).toInt() and 0xFFFF) {
                4 -> readFormat4(b, sub, result)
                12 -> readFormat12(b, sub, result)
            }
        }
        return result
    }

    private fun u16(b: ByteBuffer, pos: Int) = b.getShort(pos).toInt() and 0xFFFF

    private fun readFormat4(b: ByteBuffer, sub: Int, out: MutableSet<Int>) {
        val segCount = u16(b, sub + 6) / 2
        val endCodes = sub + 14
        val startCodes = endCodes + segCount * 2 + 2
        val idDeltas = startCodes + segCount * 2
        val idRangeOffsets = idDeltas + segCount * 2
        for (s in 0 until segCount) {
            val end = u16(b, endCodes + s * 2)
            val start = u16(b, startCodes + s * 2)
            val delta = u16(b, idDeltas + s * 2)
            val rangeOffsetPos = idRangeOffsets + s * 2
            val rangeOffset = u16(b, rangeOffsetPos)
            for (c in start..end) {
                if (c == 0xFFFF) continue
                val glyph = if (rangeOffset == 0) {
                    (c + delta) and 0xFFFF
                } else {
                    val g = u16(b, rangeOffsetPos + rangeOffset + 2 * (c - start))
                    if (g == 0) 0 else (g + delta) and 0xFFFF
                }
                if (glyph != 0) out.add(c)
            }
        }
    }

    private fun readFormat12(b: ByteBuffer, sub: Int, out: MutableSet<Int>) {
        val numGroups = b.getInt(sub + 12)
        for (g in 0 until numGroups) {
            val p = sub + 16 + g * 12
            val start = b.getInt(p)
            val end = b.getInt(p + 4)
            val startGlyph = b.getInt(p + 8)
            for (c in start..end) if (startGlyph + (c - start) != 0) out.add(c)
        }
    }
}
