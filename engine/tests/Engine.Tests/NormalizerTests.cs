using Engine.Geometry;

namespace Engine.Tests;

public class NormalizerTests
{
    [Fact]
    public void Center_BoxMapsToExpectedFractions()
    {
        var n = Normalizer.Normalize(new Box(960, 540, 96, 54), new ContentRect(0, 0, 1920, 1080));
        Assert.Equal(0.5, n.X, 9);
        Assert.Equal(0.5, n.Y, 9);
        Assert.Equal(0.05, n.W, 9);
        Assert.Equal(0.05, n.H, 9);
    }

    [Fact]
    public void RectWithOffset_SubtractsOriginBeforeDividing()
    {
        var n = Normalizer.Normalize(new Box(100, 160, 192, 108), new ContentRect(0, 60, 1920, 1080));
        Assert.Equal(100.0 / 1080, n.Y, 9);
        Assert.Equal(100.0 / 1920, n.X, 9);
        Assert.Equal(192.0 / 1920, n.W, 9);
        Assert.Equal(108.0 / 1080, n.H, 9);
    }

    [Fact]
    public void BoxOverflowingEdge_ClampsCornersAndKeepsSizeNonNegative()
    {
        var rect = new ContentRect(0, 0, 1920, 1080);
        var n = Normalizer.Normalize(new Box(1900, 1000, 100, 200), rect);
        Assert.InRange(n.X, 0, 1);
        Assert.InRange(n.Y, 0, 1);
        Assert.True(n.W >= 0 && n.H >= 0);
        Assert.True(n.X + n.W <= 1.0 + 1e-12);
        Assert.True(n.Y + n.H <= 1.0 + 1e-12);
        Assert.Equal(20.0 / 1920, n.W, 9);
        Assert.Equal(80.0 / 1080, n.H, 9);

        // Hoàn toàn ngoài mép trái/trên ⇒ w,h = 0, không âm.
        var out0 = Normalizer.Normalize(new Box(-300, -50, 100, 20), rect);
        Assert.Equal(0, out0.X);
        Assert.Equal(0, out0.W);
        Assert.Equal(0, out0.Y);
        Assert.Equal(0, out0.H);
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    public void EmptyRect_ThrowsWithMessage_NoDivideByZero(int w, int h)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Normalizer.Normalize(new Box(1, 1, 1, 1), new ContentRect(0, 0, w, h)));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }
}
