package vn.vietsub.ps5.tv

import org.junit.Assert.assertEquals
import org.junit.Test

class CoordinateMapperTest {
    @Test
    fun fourCorners_onFullHdView() {
        assertEquals(PixelPoint(0f, 0f), CoordinateMapper.toPixel(0f, 0f, 1920, 1080))
        assertEquals(PixelPoint(1920f, 0f), CoordinateMapper.toPixel(1f, 0f, 1920, 1080))
        assertEquals(PixelPoint(0f, 1080f), CoordinateMapper.toPixel(0f, 1f, 1920, 1080))
        assertEquals(PixelPoint(1920f, 1080f), CoordinateMapper.toPixel(1f, 1f, 1920, 1080))
    }

    @Test
    fun differentViewSize_centerMapsToHalf() {
        assertEquals(PixelPoint(1920f, 1080f), CoordinateMapper.toPixel(0.5f, 0.5f, 3840, 2160))
    }

    @Test
    fun outOfRange_isClampedToViewBounds() {
        assertEquals(PixelPoint(0f, 1080f), CoordinateMapper.toPixel(-0.1f, 1.2f, 1920, 1080))
    }
}
