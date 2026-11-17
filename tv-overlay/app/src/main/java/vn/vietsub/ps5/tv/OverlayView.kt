package vn.vietsub.ps5.tv

import android.content.Context
import android.graphics.Canvas
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.Typeface
import android.os.SystemClock
import android.view.View

/** Đo chữ bằng Paint (Be Vietnam Pro Regular/SemiBold) cho [AutoFit]. */
class PaintMeasurer(private val regular: Typeface, private val semibold: Typeface) : TextMeasurer {
    private val paint = Paint(Paint.ANTI_ALIAS_FLAG)

    private fun prep(size: Float, bold: Boolean): Paint = paint.apply { typeface = if (bold) semibold else regular; textSize = size }

    override fun width(text: String, sizePx: Float, bold: Boolean) = prep(sizePx, bold).measureText(text)

    override fun lineHeight(sizePx: Float, bold: Boolean): Float {
        val fm = prep(sizePx, bold).fontMetrics
        return (fm.descent - fm.ascent) * 1.05f
    }
}

/**
 * Lớp phủ sản phẩm: vẽ [OverlayModel] (D11): map box → pixel (CoordinateMapper), auto-fit, vùng che C1
 * (hợp bbox gốc ∪ bbox chữ + lề ≥ 6/1080, không ra ngoài room), fade 120/80 ms, ttl.
 */
class OverlayView(context: Context, private val model: OverlayModel) : View(context) {
    private val regular: Typeface = resources.getFont(R.font.be_vietnam_pro_regular)
    private val semibold: Typeface = resources.getFont(R.font.be_vietnam_pro_semibold)
    private val measurer = PaintMeasurer(regular, semibold)
    private val fill = Paint(Paint.ANTI_ALIAS_FLAG).apply { style = Paint.Style.FILL }
    private val text = Paint(Paint.ANTI_ALIAS_FLAG)

    private var cachedVersion = -1
    private var cachedW = 0
    private var cachedH = 0
    private var fits: Map<String, FitResult> = emptyMap()
    private val rectF = RectF()

    private fun rect(b: Box, w: Int, h: Int): Rect {
        val tl = CoordinateMapper.toPixel(b.x, b.y, w, h)
        val br = CoordinateMapper.toPixel(b.x + b.w, b.y + b.h, w, h)
        return Rect(tl.x, tl.y, br.x, br.y)
    }

    private fun relayout(items: List<OverlayItem>, w: Int, h: Int) {
        val af = AutoFit(measurer, w.toFloat(), h.toFloat(), model.display().minFontPx) { android.util.Log.w("VietSubOverlay", it) }
        val inputs = items.map { FitInput(it, rect(it.box, w, h), it.room?.let { r -> rect(r, w, h) }) }
        fits = af.fit(inputs).associateBy { it.item.id }
        cachedVersion = model.version; cachedW = w; cachedH = h
    }

    private fun withAlpha(argb: Int, a: Float): Int {
        val na = ((argb ushr 24) * a).toInt().coerceIn(0, 255)
        return (na shl 24) or (argb and 0x00FFFFFF)
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        val w = width
        val h = height
        if (w <= 0 || h <= 0) return
        val now = SystemClock.uptimeMillis()
        val snap = model.snapshot(now)
        if (snap.isNotEmpty() && (model.version != cachedVersion || w != cachedW || h != cachedH)) relayout(snap.map { it.item }, w, h)
        val plane = Rect(0f, 0f, w.toFloat(), h.toFloat())
        val u = h / 1080f
        val margin = CoverRegion.minMargin(h.toFloat())

        for (v in snap) {
            val fr = fits[v.item.id] ?: continue
            val a = v.alpha
            if (a <= 0f) continue
            val cover = CoverRegion.compute(fr.boxPx, fr.textRect, margin, fr.roomPx, plane)
            when (v.item.cover.mode) {
                "solid" -> {
                    // viền mờ 2 px: một lớp rộng hơn 2 px ở alpha thấp, rồi lớp đặc
                    fill.color = withAlpha(v.item.cover.color, a * 0.45f)
                    rectF.set(cover.l - 2f * u, cover.t - 2f * u, cover.r + 2f * u, cover.b + 2f * u)
                    canvas.drawRoundRect(rectF, 2f * u, 2f * u, fill)
                    fill.color = withAlpha(v.item.cover.color, a)
                    rectF.set(cover.l, cover.t, cover.r, cover.b)
                    canvas.drawRect(rectF, fill)
                }
                "card" -> {
                    fill.color = withAlpha(CoverRegion.opaque(v.item.cover.color), a)   // đục 100% (C1/N2)
                    rectF.set(cover.l, cover.t, cover.r, cover.b)
                    canvas.drawRoundRect(rectF, 8f * u, 8f * u, fill)
                }
                else -> Unit
            }
            text.typeface = if (v.item.bold) semibold else regular
            text.textSize = fr.sizePx
            text.color = withAlpha(v.item.textColor, a)
            if (v.item.cover.mode == "none") text.setShadowLayer(3f * u, 0f, 1f * u, withAlpha(0xFF000000.toInt(), a)) else text.clearShadowLayer()
            val lh = measurer.lineHeight(fr.sizePx, v.item.bold)
            val fm = text.fontMetrics
            fr.lines.forEachIndexed { i, line ->
                val tw = text.measureText(line)
                val x = when (v.item.align) {
                    "center" -> (fr.textRect.l + fr.textRect.r) / 2f - tw / 2f
                    "end" -> fr.textRect.r - tw
                    else -> fr.textRect.l
                }
                val baseline = fr.textRect.t + i * lh + (lh - (fm.descent - fm.ascent)) / 2f - fm.ascent
                canvas.drawText(line, x, baseline, text)
            }
        }

        if (model.isAnimating(now)) postInvalidateOnAnimation()
        else model.nextEventMs(now)?.let { postInvalidateDelayed((it - now).coerceAtLeast(1L)) }
    }

    /** Có gói/ trạng thái mới ⇒ vẽ lại. */
    fun onModelChanged() = postInvalidateOnAnimation()
}
