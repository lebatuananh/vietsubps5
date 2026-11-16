package vn.vietsub.ps5.tv

data class PixelPoint(val x: Float, val y: Float)

/** Hàm thuần: toạ độ chuẩn hoá 0..1 → pixel của View. Giá trị ngoài miền bị kẹp về [0,1]. */
object CoordinateMapper {
    fun toPixel(nx: Float, ny: Float, width: Int, height: Int): PixelPoint =
        PixelPoint(nx.coerceIn(0f, 1f) * width, ny.coerceIn(0f, 1f) * height)
}
