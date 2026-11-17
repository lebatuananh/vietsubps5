package vn.vietsub.ps5.tv

import kotlin.math.max
import kotlin.math.min

/** Hình chữ nhật thuần (không phụ thuộc android.graphics để test JVM). */
data class Rect(val l: Float, val t: Float, val r: Float, val b: Float) {
    val w: Float get() = r - l
    val h: Float get() = b - t
}

interface TextMeasurer {
    fun width(text: String, sizePx: Float, bold: Boolean): Float
    fun lineHeight(sizePx: Float, bold: Boolean): Float
}

/** Kết quả vẽ một item: chữ đã xuống dòng, cỡ, vùng chữ, có dùng `alt` không. */
data class FitResult(
    val item: OverlayItem,
    val lines: List<String>,
    val sizePx: Float,
    val textRect: Rect,
    val usedAlt: Boolean,
    val boxPx: Rect,
    val roomPx: Rect?,
)

data class FitInput(val item: OverlayItem, val boxPx: Rect, val roomPx: Rect?)

/**
 * Auto-fit (D11/N5/N12): từ `fontPx` giảm tới cỡ tối thiểu, xuống dòng trong box rồi trong `room`, tăng dòng tới maxLines;
 * cùng `group` ⇒ cùng cỡ = min; nhãn hàng ngang thiếu khe (&lt; 2% bề rộng) dùng `alt`, còn thiếu ⇒ co cả nhóm.
 * Cắt "…" CHỈ cho đoạn văn (maxLines &gt; 1), không bao giờ cho nhãn.
 */
class AutoFit(
    private val m: TextMeasurer,
    private val planeW: Float,
    private val planeH: Float,
    private val minFontPx1080: Int,
    private val onEllipsis: ((String) -> Unit)? = null,
) {
    private val scale = planeH / 1080f
    private val minSize = minFontPx1080 * scale
    private val floorSize = FLOOR_FACTOR * minSize
    private val eps = 0.01f

    companion object { const val FLOOR_FACTOR = 0.85f }

    private class Layout(val lines: List<String>, val size: Float)

    fun fit(inputs: List<FitInput>): List<FitResult> {
        val n = inputs.size
        val size = FloatArray(n)
        val text = Array(n) { inputs[it].item.text }
        val usedAlt = BooleanArray(n)

        // 1) cỡ riêng từng item (chữ đầy đủ)
        for (i in 0 until n) size[i] = individualSize(inputs[i], text[i], max(inputs[i].item.fontPx * scale, minSize))

        // 1b) nhãn không vừa kể cả ở sàn mà có `alt` ngắn hơn vừa ⇒ dùng `alt` (trước khi phải cắt "…")
        for (i in 0 until n) {
            val alt = inputs[i].item.alt
            if (size[i] <= floorSize + eps && !alt.isNullOrEmpty() && fitsAt(inputs[i], text[i], floorSize) == null
                && fitsAt(inputs[i], alt, floorSize) != null) {
                usedAlt[i] = true
                size[i] = individualSize(inputs[i], alt, max(inputs[i].item.fontPx * scale, minSize))
            }
        }

        // 2) cùng nhóm ⇒ cùng cỡ = min; nhóm hàng ngang xử lý alt/ co cả nhóm
        val groups = inputs.indices.filter { !inputs[it].item.group.startsWith("solo-") }.groupBy { inputs[it].item.group }
        for ((name, idx) in groups) {
            var s = idx.minOf { size[it] }
            if (name.startsWith("row-") && idx.size >= 2) {
                val order = idx.sortedBy { inputs[it].boxPx.l }
                val minGap = 0.02f * planeW
                while (true) {
                    val lack = ArrayList<Int>()
                    for (k in 0 until order.size - 1) {
                        val i = order[k]
                        val txt = if (usedAlt[i]) inputs[i].item.alt!! else inputs[i].item.text
                        val rect = rectFor(inputs[i], forceLayout(inputs[i], txt, s))
                        if (inputs[order[k + 1]].boxPx.l - rect.r < minGap) lack.add(i)
                    }
                    if (lack.isEmpty()) break
                    val fresh = lack.filter { !usedAlt[it] && !inputs[it].item.alt.isNullOrEmpty() }
                    if (fresh.isNotEmpty()) { fresh.forEach { usedAlt[it] = true }; continue }
                    if (s - 1f >= floorSize - eps) s -= 1f else break
                }
            }
            idx.forEach { size[it] = s }
        }

        return inputs.indices.map { i ->
            val txt = if (usedAlt[i]) inputs[i].item.alt!! else text[i]
            val lay = forceLayout(inputs[i], txt, size[i])
            FitResult(inputs[i].item, lay.lines, size[i], rectFor(inputs[i], lay), usedAlt[i], inputs[i].boxPx, inputs[i].roomPx)
        }
    }

    // ---------------------------------------------------------------- bố cục một item

    /** Số dòng "gốc": nhãn = 1; đoạn văn engine gửi maxLines = số dòng gốc + 2. */
    private fun baseLines(inp: FitInput): Int = if (inp.item.maxLines <= 1) 1 else max(1, inp.item.maxLines - 2)

    /** Dãy cỡ giảm dần từ [from] tới đúng [to] (bước 1 px). */
    private fun sizes(from: Float, to: Float): Sequence<Float> = sequence {
        var s = max(from, to)
        while (s > to + eps) { yield(s); s -= 1f }
        yield(to)
    }

    /** Có bố cục KHÔNG cắt "…" ở cỡ [size] theo đúng thứ tự: số dòng gốc trước, thêm dòng (trong chiều cao room) sau? */
    private fun fitsAt(inp: FitInput, text: String, size: Float): Layout? {
        tryLayout(inp, text, size, baseLines(inp), checkHeight = false)?.let { return it }
        if (inp.item.maxLines > baseLines(inp)) tryLayout(inp, text, size, inp.item.maxLines, checkHeight = true)?.let { return it }
        return null
    }

    /** Cỡ lớn nhất ≤ [start] mà chữ vừa theo thứ tự Rev 1.4; không vừa ⇒ sàn 0,85 × min. */
    private fun individualSize(inp: FitInput, text: String, start: Float): Float {
        val base = baseLines(inp)
        // 1) co tới cỡ tối thiểu với số dòng gốc
        for (s in sizes(start, minSize)) if (tryLayout(inp, text, s, base, checkHeight = false) != null) return s
        // 3) thêm dòng, chỉ trong chiều cao room
        if (inp.item.maxLines > base)
            for (s in sizes(start, minSize)) if (tryLayout(inp, text, s, inp.item.maxLines, checkHeight = true) != null) return s
        // 4) co tiếp tới sàn
        for (s in sizes(minSize - 1f, floorSize)) if (fitsAt(inp, text, s) != null) return s
        return floorSize
    }

    private fun widthsToTry(inp: FitInput): List<Float> {
        val box = inp.boxPx.w
        val avail = availWidth(inp)
        return if (avail > box + eps) listOf(box, avail) else listOf(box)
    }

    private fun availWidth(inp: FitInput): Float {
        val room = inp.roomPx ?: return inp.boxPx.w
        val b = inp.boxPx
        return when (inp.item.align) {
            "end" -> b.r - room.l
            "center" -> 2f * min((b.l + b.r) / 2f - room.l, room.r - (b.l + b.r) / 2f)
            else -> room.r - b.l
        }.coerceAtLeast(b.w)
    }

    private fun heightLimit(inp: FitInput): Float = inp.roomPx?.h ?: inp.boxPx.h

    private fun tryLayout(inp: FitInput, text: String, size: Float, maxL: Int, checkHeight: Boolean): Layout? {
        val bold = inp.item.bold
        var best: Layout? = null
        for (w in widthsToTry(inp)) {
            val lines: List<String>? = if (maxL == 1) {
                if (m.width(text, size, bold) <= w + eps) listOf(text) else null
            } else {
                wrap(text, size, bold, w, force = false)?.takeIf { it.size <= maxL }
            }
            if (lines == null) continue
            if (checkHeight && lines.size > 1 && lines.size * m.lineHeight(size, bold) > heightLimit(inp) + eps) continue
            // ít dòng nhất trong các bề rộng cho phép (box rồi room); hoà ⇒ giữ bề rộng hẹp hơn
            if (best == null || lines.size < best.lines.size) best = Layout(lines, size)
        }
        return best
    }

    /** Bố cục ở cỡ cố định: vừa thì dùng; không vừa ⇒ phương án cuối: cắt "…" (nhãn một dòng cũng vậy). */
    private fun forceLayout(inp: FitInput, text: String, size: Float): Layout {
        fitsAt(inp, text, size)?.let { return it }
        val bold = inp.item.bold
        val w = widthsToTry(inp).last()
        if (inp.item.maxLines <= 1) {
            onEllipsis?.invoke("nhãn \"${inp.item.id}\" không vừa một dòng ở cỡ ${size.toInt()} — cắt …")
            return Layout(listOf(ellipsize(text, size, bold, w)), size)
        }
        val lines = wrap(text, size, bold, w, force = true)!!
        if (lines.size <= inp.item.maxLines) return Layout(lines, size)
        val kept = lines.take(inp.item.maxLines).toMutableList()
        kept[kept.lastIndex] = ellipsize(kept.last() + " " + lines.drop(inp.item.maxLines).joinToString(" "), size, bold, w)
        onEllipsis?.invoke("đoạn \"${inp.item.id}\" không vừa ${inp.item.maxLines} dòng ở cỡ ${size.toInt()} — cắt …")
        return Layout(kept, size)
    }

    private fun ellipsize(s: String, size: Float, bold: Boolean, w: Float): String {
        var t = s.trimEnd()
        while (t.isNotEmpty() && m.width("$t…", size, bold) > w + eps) t = t.dropLast(1).trimEnd()
        return "$t…"
    }

    /** Xuống dòng theo từ; từ rộng hơn [w]: [force] ⇒ bẻ ký tự, ngược lại trả null. */
    private fun wrap(text: String, size: Float, bold: Boolean, w: Float, force: Boolean): List<String>? {
        val words = text.split(' ').filter { it.isNotEmpty() }
        if (words.isEmpty()) return listOf("")
        val lines = ArrayList<String>()
        var cur = ""
        fun flush() { if (cur.isNotEmpty()) { lines.add(cur); cur = "" } }
        for (word in words) {
            var piece = word
            if (m.width(piece, size, bold) > w + eps) {
                if (!force) return null
                flush()
                while (m.width(piece, size, bold) > w + eps && piece.length > 1) {
                    var cut = piece.length - 1
                    while (cut > 1 && m.width(piece.substring(0, cut), size, bold) > w + eps) cut--
                    lines.add(piece.substring(0, cut)); piece = piece.substring(cut)
                }
            }
            val cand = if (cur.isEmpty()) piece else "$cur $piece"
            if (cur.isNotEmpty() && m.width(cand, size, bold) > w + eps) { flush(); cur = piece } else cur = cand
        }
        flush()
        return lines
    }

    /** Vùng chữ theo align, canh giữa dọc trong box, kéo vào trong room nếu lệch. */
    private fun rectFor(inp: FitInput, lay: Layout): Rect {
        val b = inp.boxPx
        val bold = inp.item.bold
        val tw = lay.lines.maxOf { m.width(it, lay.size, bold) }
        val th = lay.lines.size * m.lineHeight(lay.size, bold)
        var l = when (inp.item.align) {
            "center" -> (b.l + b.r) / 2f - tw / 2f
            "end" -> b.r - tw
            else -> b.l
        }
        var t = b.t + max(0f, (b.h - th) / 2f)
        inp.roomPx?.let { room ->
            if (t + th > room.b) t = room.b - th        // nhiều dòng: không vượt đáy room
            if (t < room.t) t = room.t
            if (l + tw > room.r) l = room.r - tw
            if (l < room.l) l = room.l
        }
        return Rect(l, t, l + tw, t + th)
    }
}

object CoverRegion {
    /** Hợp(bbox gốc, bbox chữ) + lề ≥ [marginPx]; không ra ngoài [roomPx] (nếu có) và [plane]. */
    fun compute(boxPx: Rect, textRect: Rect, marginPx: Float, roomPx: Rect?, plane: Rect): Rect {
        var r = Rect(
            min(boxPx.l, textRect.l) - marginPx, min(boxPx.t, textRect.t) - marginPx,
            max(boxPx.r, textRect.r) + marginPx, max(boxPx.b, textRect.b) + marginPx,
        )
        if (roomPx != null) r = Rect(max(r.l, roomPx.l), max(r.t, roomPx.t), min(r.r, roomPx.r), min(r.b, roomPx.b))
        r = Rect(min(r.l, boxPx.l), min(r.t, boxPx.t), max(r.r, boxPx.r), max(r.b, boxPx.b)) // luôn phủ bbox gốc (C1)
        return Rect(max(r.l, plane.l), max(r.t, plane.t), min(r.r, plane.r), min(r.b, plane.b))
    }

    /** Lề tối thiểu = 6/1080 chiều cao màn. */
    fun minMargin(planeH: Float): Float = 6f * planeH / 1080f

    /** card = nền ĐỤC 100%: ép alpha = 0xFF, giữ RGB. */
    fun opaque(argb: Int): Int = argb or 0xFF000000.toInt()
}
