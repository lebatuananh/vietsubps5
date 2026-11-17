package vn.vietsub.ps5.tv

import com.google.gson.JsonElement
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import java.io.BufferedInputStream
import java.io.ByteArrayOutputStream
import java.io.IOException
import java.io.InputStream

// Nguồn chân lý: tv-overlay/README.md mục "Giao thức" (v1).

data class Box(val x: Float, val y: Float, val w: Float, val h: Float)

/** [color] = ARGB (Int) đã parse từ chuỗi #AARRGGBB. [mode] solid|card|none. */
data class CoverSpec(val mode: String, val color: Int)

data class OverlayItem(
    val id: String,
    val box: Box,
    val text: String,
    val alt: String?,
    val fontPx: Int,
    val group: String,
    val maxLines: Int,
    val align: String,
    val bold: Boolean,
    val textColor: Int,
    val cover: CoverSpec,
    val room: Box?,
)

data class Display(val minFontPx: Int, val holdMs: Int)

data class OverlayFrame(
    val sessionId: String,
    val seq: Long,
    val ttlMs: Int,
    val display: Display?,
    val items: List<OverlayItem>,
)

data class Hello(val v: Int, val token: String)

class LineTooLongException(message: String) : IOException(message)

object Protocol {
    const val VERSION = 1
    const val PORT = 47820
    const val MAX_LINE_BYTES = 262_144
    const val MAX_ITEMS = 200
    const val MAX_TEXT = 1000
    const val MAX_TTL_MS = 60_000

    /** "#AARRGGBB" (hoa/thường) → ARGB Int; null nếu sai định dạng. */
    fun parseColor(s: String): Int? {
        if (s.length != 9 || s[0] != '#') return null
        val v = s.substring(1).toLongOrNull(16) ?: return null
        if (s.substring(1).any { Character.digit(it, 16) < 0 }) return null
        return v.toInt()
    }

    /** null nếu dòng không phải hello hợp lệ. */
    fun parseHello(line: String): Hello? {
        val root = parseObject(line) ?: return null
        val h = root.getAsObjectOrNull("hello") ?: return null
        val v = h.getIntOrNull("v") ?: return null
        val token = h.getStringOrNull("token") ?: return null
        return Hello(v, token)
    }

    /** null = chấp nhận; "version" khi v ≠ 1; "token" khi sai mã (không phân biệt hoa/thường, bỏ '-'). */
    fun checkHello(h: Hello, expectedToken: String): String? {
        if (h.v != VERSION) return "version"
        return if (PairingCode.matches(h.token, expectedToken)) null else "token"
    }

    fun replyOk(): String = "{\"ok\":true}"

    fun replyReject(reason: String): String = "{\"ok\":false,\"reason\":\"$reason\"}"

    /** null ⇒ TV bỏ gói (v ≠ 1, thiếu trường, vi phạm giới hạn). */
    fun parseFrame(line: String): OverlayFrame? {
        val root = parseObject(line) ?: return null
        if (root.getIntOrNull("v") != VERSION) return null
        val session = root.getStringOrNull("sessionId")?.takeIf { it.isNotEmpty() } ?: return null
        val seq = root.getLongOrNull("seq") ?: return null
        val ttl = root.getIntOrNull("ttlMs") ?: return null
        if (ttl < 1 || ttl > MAX_TTL_MS) return null
        val display = root.getAsObjectOrNull("display")?.let { d ->
            val min = d.getIntOrNull("minFontPx")
            val hold = d.getIntOrNull("holdMs")
            if (min != null && hold != null) Display(min, hold.coerceAtLeast(0)) else null
        }
        val arr = root.get("items")?.takeIf { it.isJsonArray }?.asJsonArray ?: return null
        if (arr.size() > MAX_ITEMS) return null
        val items = ArrayList<OverlayItem>(arr.size())
        for (el in arr) items.add(parseItem(el) ?: return null)
        return OverlayFrame(session, seq, ttl, display, items)
    }

    private fun parseItem(el: JsonElement): OverlayItem? {
        if (!el.isJsonObject) return null
        val o = el.asJsonObject
        val id = o.getStringOrNull("id")?.takeIf { it.isNotEmpty() } ?: return null
        val box = o.getAsObjectOrNull("box")?.let(::parseBox) ?: return null
        val text = o.getStringOrNull("text") ?: return null
        if (text.length > MAX_TEXT) return null
        val alt = o.getStringOrNull("alt")
        val fontPx = o.getIntOrNull("fontPx")?.takeIf { it >= 1 } ?: return null
        val group = o.getStringOrNull("group") ?: return null
        val maxLines = o.getIntOrNull("maxLines")?.takeIf { it in 1..6 } ?: return null
        val align = o.getStringOrNull("align")?.takeIf { it == "start" || it == "center" || it == "end" } ?: "start"
        val bold = o.get("bold")?.takeIf { it.isJsonPrimitive && it.asJsonPrimitive.isBoolean }?.asBoolean ?: false
        val textColor = o.getStringOrNull("textColor")?.let(::parseColor) ?: return null
        val coverObj = o.getAsObjectOrNull("cover") ?: return null
        val mode = coverObj.getStringOrNull("mode")?.takeIf { it == "solid" || it == "card" || it == "none" } ?: return null
        val coverColor = coverObj.getStringOrNull("color")?.let(::parseColor) ?: return null
        val roomEl = o.get("room")
        val room = if (roomEl == null || roomEl.isJsonNull) null else (roomEl.takeIf { it.isJsonObject }?.asJsonObject?.let(::parseBox) ?: return null)
        return OverlayItem(id, box, text, alt, fontPx, group, maxLines, align, bold, textColor, CoverSpec(mode, coverColor), room)
    }

    private fun parseBox(o: JsonObject): Box? {
        val x = o.getFloatOrNull("x") ?: return null
        val y = o.getFloatOrNull("y") ?: return null
        val w = o.getFloatOrNull("w") ?: return null
        val h = o.getFloatOrNull("h") ?: return null
        return Box(x, y, w, h)
    }

    private fun parseObject(line: String): JsonObject? = try {
        JsonParser.parseString(line).takeIf { it.isJsonObject }?.asJsonObject
    } catch (e: Exception) {
        null
    }

    private fun JsonObject.getAsObjectOrNull(k: String): JsonObject? = get(k)?.takeIf { it.isJsonObject }?.asJsonObject

    private fun JsonObject.getStringOrNull(k: String): String? =
        get(k)?.takeIf { it.isJsonPrimitive && it.asJsonPrimitive.isString }?.asString

    private fun JsonObject.getNumber(k: String) = get(k)?.takeIf { it.isJsonPrimitive && it.asJsonPrimitive.isNumber }?.asBigDecimal

    private fun JsonObject.getIntOrNull(k: String): Int? = getNumber(k)?.let { runCatching { it.intValueExact() }.getOrNull() }

    private fun JsonObject.getLongOrNull(k: String): Long? = getNumber(k)?.let { runCatching { it.longValueExact() }.getOrNull() }

    private fun JsonObject.getFloatOrNull(k: String): Float? = getNumber(k)?.toFloat()
}

/** `seq` ≤ seq đã xử lý cùng `sessionId` ⇒ bỏ; sessionId khác ⇒ phiên mới, đặt lại. */
class SeqGate {
    private var session: String? = null
    private var last: Long = Long.MIN_VALUE

    @Synchronized
    fun accept(f: OverlayFrame): Boolean {
        if (f.sessionId != session) {
            session = f.sessionId
            last = f.seq
            return true
        }
        if (f.seq > last) { last = f.seq; return true }
        return false
    }
}

/** Đọc từng dòng UTF-8 kết thúc '\n', dài tối đa [maxBytes] (không tính '\n'); vượt ⇒ [LineTooLongException]. null khi EOF. */
class BoundedLineReader(input: InputStream, private val maxBytes: Int = Protocol.MAX_LINE_BYTES) {
    private val src = BufferedInputStream(input, 8192)

    fun readLine(): String? {
        val buf = ByteArrayOutputStream()
        while (true) {
            val b = src.read()
            if (b < 0) return null // EOF (phần dở dang bị bỏ)
            if (b == '\n'.code) return buf.toString(Charsets.UTF_8.name())
            if (buf.size() >= maxBytes) throw LineTooLongException("dòng dài hơn $maxBytes byte")
            buf.write(b)
        }
    }
}
