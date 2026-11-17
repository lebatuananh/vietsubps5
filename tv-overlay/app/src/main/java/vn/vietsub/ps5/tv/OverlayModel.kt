package vn.vietsub.ps5.tv

data class Visible(val item: OverlayItem, val alpha: Float)

/**
 * Mô hình trạng thái lớp phủ (thuần, đồng hồ do người gọi cấp): item mới fade-in 120 ms, biến mất fade-out 80 ms
 * sau `holdMs` (do TV áp), `items:[]` ⇒ xoá, hết `ttlMs` không gói ⇒ xoá, cùng `id` + nội dung y hệt ⇒ không vẽ lại.
 */
class OverlayModel(private val onWarn: (String) -> Unit = {}) {
    private class Entry(var item: OverlayItem, var appearedAt: Long, var removeAt: Long? = null)

    private val entries = LinkedHashMap<String, Entry>()
    private var lastDisplay = Display(24, 0)
    private var ttlDeadline = 0L
    private var ttlHandled = true

    var lastFrame: OverlayFrame? = null
        private set

    /** Số lần bộ item thay đổi (để view biết cần tính lại bố cục). */
    var version: Int = 0
        private set

    private fun gone(e: Entry, now: Long): Boolean = e.removeAt?.let { now >= it + FADE_OUT_MS } ?: false

    private fun purge(now: Long) {
        val it = entries.values.iterator()
        while (it.hasNext()) if (gone(it.next(), now)) { it.remove(); version++ }
    }

    private fun alpha(e: Entry, now: Long): Float {
        val ra = e.removeAt
        if (ra != null && now >= ra) return (1f - (now - ra).toFloat() / FADE_OUT_MS).coerceIn(0f, 1f)
        return ((now - e.appearedAt).toFloat() / FADE_IN_MS).coerceIn(0f, 1f)
    }

    private fun tickTtl(now: Long) {
        if (!ttlHandled && now > ttlDeadline) {
            for (e in entries.values) if (e.removeAt == null) e.removeAt = ttlDeadline
            ttlHandled = true
            version++
        }
    }

    fun applyFrame(f: OverlayFrame, nowMs: Long) {
        tickTtl(nowMs)
        purge(nowMs)
        val display = f.display ?: lastDisplay
        lastDisplay = display
        ttlDeadline = nowMs + f.ttlMs
        ttlHandled = false
        lastFrame = f

        val ids = HashSet<String>()
        for (item in f.items) {
            // README: `id` duy nhất trong một gói (engine đảm bảo). Nếu vẫn trùng: GIỮ item đầu, bỏ phần còn lại và log (Rev 1.4).
            if (!ids.add(item.id)) { onWarn("id trùng trong gói seq=${f.seq}: \"${item.id}\" — giữ item đầu, bỏ item sau"); continue }
            val key = item.id
            val e = entries[key]
            if (e == null) {
                entries[key] = Entry(item, nowMs)
                version++
                continue
            }
            if (e.item != item) { e.item = item; version++ }
            if (e.removeAt != null) {                       // đang chờ gỡ/ fade-out mà lại có mặt ⇒ giữ hiện
                val a = alpha(e, nowMs)
                e.removeAt = null
                e.appearedAt = nowMs - (a * FADE_IN_MS).toLong()
                version++
            }
        }
        for ((id, e) in entries) {
            if (id !in ids && e.removeAt == null) { e.removeAt = nowMs + display.holdMs.toLong(); version++ }
        }
    }

    /** Các item đang hiện + độ mờ tại [nowMs] (đã xử lý ttl/hold). */
    fun snapshot(nowMs: Long): List<Visible> {
        tickTtl(nowMs)
        purge(nowMs)
        return entries.values.map { Visible(it.item, alpha(it, nowMs)) }
    }

    /** Còn animation/ hạn chờ cần vẽ tiếp? */
    fun isAnimating(nowMs: Long): Boolean {
        tickTtl(nowMs)
        purge(nowMs)
        return entries.values.any { it.removeAt != null || nowMs - it.appearedAt < FADE_IN_MS }
    }

    /** Thời điểm sớm nhất cần vẽ lại (hạn ttl/ bắt đầu gỡ); null nếu không có gì chờ. */
    fun nextEventMs(nowMs: Long): Long? {
        tickTtl(nowMs)
        purge(nowMs)
        if (entries.isEmpty()) return null
        var best: Long? = if (!ttlHandled) ttlDeadline else null
        for (e in entries.values) {
            val ra = e.removeAt ?: continue
            if (ra > nowMs && (best == null || ra < best)) best = ra
        }
        return best
    }

    fun display(): Display = lastDisplay

    companion object {
        const val FADE_IN_MS = 120L
        const val FADE_OUT_MS = 80L
    }
}
