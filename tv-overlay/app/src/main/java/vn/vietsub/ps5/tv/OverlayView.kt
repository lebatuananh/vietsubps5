package vn.vietsub.ps5.tv

import android.content.Context
import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.Typeface
import android.text.Layout
import android.text.StaticLayout
import android.text.TextPaint
import android.util.TypedValue
import android.view.View

/**
 * View tự vẽ cho lớp phủ kiểm tra: nền trong suốt, 4 dấu góc chữ L (đỉnh đúng góc, hai cạnh vẽ vào trong),
 * lưới theo 10%, thẻ chữ Việt mẫu ở 3 cỡ, kích thước View, dòng "lớp phủ kiểm tra" góc dưới phải.
 * Mọi vị trí đi qua [CoordinateMapper] (toạ độ chuẩn hoá của View).
 */
class OverlayView(context: Context) : View(context) {
    private val regular: Typeface = resources.getFont(R.font.be_vietnam_pro_regular)
    private val semibold: Typeface = resources.getFont(R.font.be_vietnam_pro_semibold)

    private val markPaint = Paint().apply { color = Color.parseColor("#FFD400"); style = Paint.Style.FILL }
    private val gridPaint = Paint().apply {
        color = Color.argb(77, 255, 255, 255) // ~30% đục
        strokeWidth = 1f
        style = Paint.Style.STROKE
    }
    private val cardPaint = Paint().apply { color = Color.argb(204, 0, 0, 0); style = Paint.Style.FILL } // ~80% đục
    private val footerBg = Paint().apply { color = Color.argb(204, 0, 0, 0); style = Paint.Style.FILL }

    private fun sp(v: Float) = TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_SP, v, resources.displayMetrics)
    private fun dp(v: Float) = TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, v, resources.displayMetrics)

    private fun textPaint(sizeSp: Float, face: Typeface) = TextPaint(Paint.ANTI_ALIAS_FLAG).apply {
        color = Color.WHITE
        textSize = sp(sizeSp)
        typeface = face
    }

    private fun layout(text: String, paint: TextPaint, width: Int): StaticLayout =
        StaticLayout.Builder.obtain(text, 0, text.length, paint, width)
            .setAlignment(Layout.Alignment.ALIGN_NORMAL)
            .setIncludePad(false)
            .build()

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        val w = width
        val h = height
        if (w <= 0 || h <= 0) return

        // Lưới: đường ở 10%, 20%, … 90%
        for (i in 1..9) {
            val f = i / 10f
            val vx = CoordinateMapper.toPixel(f, 0f, w, h).x
            val hy = CoordinateMapper.toPixel(0f, f, w, h).y
            canvas.drawLine(vx, 0f, vx, h.toFloat(), gridPaint)
            canvas.drawLine(0f, hy, w.toFloat(), hy, gridPaint)
        }

        // 4 dấu góc chữ L: đỉnh tại (0,0),(1,0),(0,1),(1,1); hai cạnh nằm hẳn trong View
        val t = dp(6f)
        val arm = dp(64f)
        val corners = listOf(
            Triple(0f, 0f, Pair(1f, 1f)),
            Triple(1f, 0f, Pair(-1f, 1f)),
            Triple(0f, 1f, Pair(1f, -1f)),
            Triple(1f, 1f, Pair(-1f, -1f)),
        )
        for ((nx, ny, dir) in corners) {
            val p = CoordinateMapper.toPixel(nx, ny, w, h)
            val (sx, sy) = dir
            canvas.drawRect(rect(p.x, p.y, p.x + sx * arm, p.y + sy * t), markPaint) // cạnh ngang
            canvas.drawRect(rect(p.x, p.y, p.x + sx * t, p.y + sy * arm), markPaint) // cạnh dọc
        }

        // Thẻ chữ Việt mẫu ở giữa, 3 cỡ 24/32/44 sp
        val sample = context.getString(R.string.overlay_sample)
        val pad = dp(24f).toInt()
        val cardW = (w * 0.8f).toInt()
        val inner = cardW - 2 * pad
        val l1 = layout(sample, textPaint(24f, regular), inner)
        val l2 = layout(sample, textPaint(32f, regular), inner)
        val l3 = layout(sample, textPaint(44f, semibold), inner)
        val gap = dp(12f)
        val cardH = pad * 2 + l1.height + l2.height + l3.height + gap * 2
        val top = (h - cardH) / 2f
        val left = (w - cardW) / 2f
        canvas.drawRoundRect(RectF(left, top, left + cardW, top + cardH), dp(16f), dp(16f), cardPaint)
        var y = top + pad
        for (l in listOf(l1, l2, l3)) {
            canvas.save(); canvas.translate(left + pad, y); l.draw(canvas); canvas.restore()
            y += l.height + gap
        }

        // Kích thước View: giữa mép TRÊN (giữa hai dấu góc), không đè dòng chữ nhỏ góc dưới phải
        val sizePaint = textPaint(20f, regular)
        val sizeText = context.getString(R.string.overlay_size, w, h)
        val sizeW = sizePaint.measureText(sizeText)
        val sy = dp(24f) + sizePaint.textSize
        canvas.drawRect(
            RectF((w - sizeW) / 2f - dp(8f), sy - sizePaint.textSize - dp(4f), (w + sizeW) / 2f + dp(8f), sy + dp(8f)),
            footerBg,
        )
        canvas.drawText(sizeText, (w - sizeW) / 2f, sy, sizePaint)

        // Dòng nhỏ góc dưới phải (N16), chừa dấu góc
        val fp = textPaint(14f, regular)
        val foot = context.getString(R.string.overlay_footer)
        val fw = fp.measureText(foot)
        val fx = w - fw - dp(80f)
        val fy = h - dp(24f)
        canvas.drawRect(RectF(fx - dp(8f), fy - fp.textSize - dp(4f), fx + fw + dp(8f), fy + dp(8f)), footerBg)
        canvas.drawText(foot, fx, fy, fp)
    }

    private fun rect(x1: Float, y1: Float, x2: Float, y2: Float) =
        RectF(minOf(x1, x2), minOf(y1, y2), maxOf(x1, x2), maxOf(y1, y2))
}
