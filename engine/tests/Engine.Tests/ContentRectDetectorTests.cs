using Engine.Geometry;

namespace Engine.Tests;

public class ContentRectDetectorTests
{
    private const byte Gray = 128;

    /// <summary>Ảnh BGRA nền đen; vùng <paramref name="content"/> tô xám.</summary>
    private static byte[] Image(int w, int h, ContentRect content, byte gray = Gray)
    {
        var px = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = (y * w + x) * 4;
            bool inside = x >= content.X && x < content.X + content.Width
                       && y >= content.Y && y < content.Y + content.Height;
            byte v = inside ? gray : (byte)0;
            px[i] = v; px[i + 1] = v; px[i + 2] = v; px[i + 3] = 255;
        }
        return px;
    }

    private static void AssertRect(ContentRect expected, ContentRect actual, int tol = 1)
    {
        Assert.InRange(actual.X, expected.X - tol, expected.X + tol);
        Assert.InRange(actual.Y, expected.Y - tol, expected.Y + tol);
        Assert.InRange(actual.Width, expected.Width - tol, expected.Width + tol);
        Assert.InRange(actual.Height, expected.Height - tol, expected.Height + tol);
    }

    [Fact]
    public void NoBorder_ReturnsWholeImage_Confident()
    {
        var px = Image(1920, 1080, new ContentRect(0, 0, 1920, 1080));
        var r = ContentRectDetector.Detect(px, 1920, 1080, 1920 * 4);
        AssertRect(new ContentRect(0, 0, 1920, 1080), r.Rect, 0);
        Assert.True(r.Confident);
    }

    [Fact]
    public void Letterbox_TopAndBottomBlack_DetectsContent()
    {
        var px = Image(1920, 1200, new ContentRect(0, 60, 1920, 1080));
        var r = ContentRectDetector.Detect(px, 1920, 1200, 1920 * 4);
        AssertRect(new ContentRect(0, 60, 1920, 1080), r.Rect);
        Assert.True(r.Confident);
    }

    [Fact]
    public void Pillarbox_LeftAndRightBlack_DetectsContent()
    {
        var px = Image(2000, 1080, new ContentRect(40, 0, 1920, 1080));
        var r = ContentRectDetector.Detect(px, 2000, 1080, 2000 * 4);
        AssertRect(new ContentRect(40, 0, 1920, 1080), r.Rect);
        Assert.True(r.Confident);
    }

    [Fact]
    public void DarkContentEdge_WithScatteredBrightDetail_IsNotCropped()
    {
        // 2000x1080: cột đen thuần 40px mỗi bên; nội dung 1920px, trong đó 20px đầu gần đen
        // (luma 8) nhưng có chi tiết sáng rải rác (mỗi 49 hàng một vạch sáng ⇒ ~2% mỗi cột).
        const int w = 2000, h = 1080;
        var px = Image(w, h, new ContentRect(40, 0, 1920, 1080));
        for (int y = 0; y < h; y++)
        for (int x = 40; x < 60; x++)
        {
            int i = (y * w + x) * 4;
            byte v = (y % 49 == 0) ? (byte)200 : (byte)8;
            px[i] = v; px[i + 1] = v; px[i + 2] = v;
        }
        var r = ContentRectDetector.Detect(px, w, h, w * 4);
        AssertRect(new ContentRect(40, 0, 1920, 1080), r.Rect);
        Assert.True(r.Rect.X <= 41, "dải gần đen ở mép trái bị cắt mất");
    }

    [Fact]
    public void AllBlack_ReturnsWholeImage_NotConfident()
    {
        var px = new byte[1920 * 1080 * 4];
        var r = ContentRectDetector.Detect(px, 1920, 1080, 1920 * 4);
        Assert.Equal(new ContentRect(0, 0, 1920, 1080), r.Rect);
        Assert.False(r.Confident);
    }

    [Fact]
    public void ContentNotSixteenByNine_ReturnsRealRect_NotConfident()
    {
        var px = Image(1920, 1080, new ContentRect(210, 0, 1500, 1080));
        var r = ContentRectDetector.Detect(px, 1920, 1080, 1920 * 4);
        AssertRect(new ContentRect(210, 0, 1500, 1080), r.Rect);
        Assert.False(r.Confident);
        Assert.Null(r.Fallback); // viền đen đối xứng thật, không phải màn tối
    }

    private static byte[] DarkScreenWithCenterText(int w, int h)
    {
        var px = new byte[w * h * 4];
        for (int i = 0; i < px.Length; i += 4) { px[i] = 11; px[i + 1] = 11; px[i + 2] = 11; px[i + 3] = 255; }
        int bw = 600, bh = 300, bx = (w - bw) / 2, by = (h - bh) / 2;
        for (int y = by; y < by + bh; y++)
        for (int x = bx; x < bx + bw; x++)
        {
            int i = (y * w + x) * 4;
            px[i] = 200; px[i + 1] = 200; px[i + 2] = 200;
        }
        return px;
    }

    [Fact]
    public void DarkUniformScreen_TextInMiddle_NothingAtEdges_FallsBackToWholeClient()
    {
        var px = DarkScreenWithCenterText(1920, 1080);
        var r = ContentRectDetector.Detect(px, 1920, 1080, 1920 * 4);
        Assert.Equal(new ContentRect(0, 0, 1920, 1080), r.Rect);
        Assert.True(r.Confident);
        Assert.Equal("client", r.Fallback);
    }

    [Fact]
    public void DarkScreen_ClientNotSixteenByNine_NoFallback_StaysNotConfident()
    {
        var px = DarkScreenWithCenterText(1000, 1000);
        var r = ContentRectDetector.Detect(px, 1000, 1000, 1000 * 4);
        Assert.False(r.Confident);
        Assert.Null(r.Fallback);
        Assert.True(r.Rect.Width < 1000); // rect thật của vùng chữ, không bịa
    }

    [Fact]
    public void RectForNormalization_UsesWholeImage_WhenNotConfident()
    {
        var notConfident = new ContentDetection(new ContentRect(300, 148, 705, 578), false);
        Assert.Equal(new ContentRect(0, 0, 1920, 1080), notConfident.RectForNormalization(1920, 1080));
        var confident = new ContentDetection(new ContentRect(0, 60, 1920, 1080), true);
        Assert.Equal(new ContentRect(0, 60, 1920, 1080), confident.RectForNormalization(1920, 1200));
    }
}
