using Engine.Config;
using Engine.Geometry;
using Engine.Layout;

namespace Engine.Tests;

public class LayoutBuilderTests
{
    private static readonly ContentRect Content = new(0, 0, 1920, 1080);
    private static readonly TuningConfig T = new();

    private static OcrWordData W(string t, double x, double y, double w, double h) => new(t, new Box(x, y, w, h));

    private static OcrLineData Line(params OcrWordData[] words)
    {
        double x1 = words.Min(w => w.BoxPx.X), y1 = words.Min(w => w.BoxPx.Y);
        double x2 = words.Max(w => w.BoxPx.X + w.BoxPx.W), y2 = words.Max(w => w.BoxPx.Y + w.BoxPx.H);
        return new OcrLineData(string.Join(' ', words.Select(w => w.Text)), new Box(x1, y1, x2 - x1, y2 - y1), words);
    }

    /// <summary>Hàng danh sách bị OCR gộp thành MỘT dòng: người gửi · tiêu đề · ngày (khe lớn giữa các cột).</summary>
    private static OcrLineData Row(double y, string sender, string subject, string date) => Line(
        W(sender, 100, y, 14 * sender.Length, 26),
        W(subject, 560, y, 12 * subject.Length, 26),
        W(date, 1500, y, 14 * date.Length, 26));

    [Fact]
    public void MergedRow_SplitsIntoThreeBlocks_WithThreeColumnGroups()
    {
        var lines = new[]
        {
            Row(200, "Board", "Season Update", "Today"),
            Row(310, "Committee", "Contract offer", "Yesterday"),
            Row(420, "Scout", "Report ready", "12 Nov"),
        };
        var blocks = LayoutBuilder.Build(lines, Content, T, 0.78);
        Assert.Equal(9, blocks.Count);
        var groups = blocks.Select(b => b.Group).Distinct().ToList();
        Assert.Equal(3, groups.Count);
        Assert.All(groups, g => Assert.StartsWith("col-", g));
        Assert.All(blocks.GroupBy(b => b.Group), g => Assert.Equal(3, g.Count()));
        Assert.Contains(blocks, b => b.Text == "Season Update");
    }

    [Fact]
    public void ParagraphLines_AreMergedIntoOneBlock()
    {
        var lines = new[]
        {
            Line(W("Dear", 120, 250, 60, 30), W("Manager,", 190, 250, 110, 30)),
            Line(W("Your", 120, 290, 60, 30), W("offer", 190, 290, 70, 30), W("accepted.", 270, 290, 120, 30)),
            Line(W("Please", 121, 330, 90, 30), W("sign.", 220, 330, 70, 30)),
        };
        var b = Assert.Single(LayoutBuilder.Build(lines, Content, T, 0.78));
        Assert.Equal("Dear Manager, Your offer accepted. Please sign.", b.Text);
        Assert.Equal(3, b.LineCount);
        Assert.Equal(5, b.MaxLines); // số dòng gốc + 2
    }

    [Fact]
    public void FarApartLines_AreNotMerged()
    {
        var lines = new[] { Line(W("Inbox", 80, 50, 130, 40)), Line(W("Board", 100, 200, 92, 26)) };
        Assert.Equal(2, LayoutBuilder.Build(lines, Content, T, 0.78).Count);
    }

    [Fact]
    public void NoiseLines_AreDropped()
    {
        var lines = new[]
        {
            Line(W("12", 100, 100, 30, 26)),
            Line(W("△", 100, 150, 30, 26)),
            Line(W("R1", 300, 150, 30, 26)),
            Line(W("A", 500, 150, 14, 26)),
            Line(W("€45.0M", 700, 150, 90, 26)),
            Line(W("Scout Report", 100, 600, 200, 26)),
        };
        var blocks = LayoutBuilder.Build(lines, Content, T, 0.78);
        Assert.Equal(new[] { "Scout Report" }, blocks.Select(b => b.Text).ToArray());
    }

    [Fact]
    public void SingleRowOfTightLabels_GetOneRowGroup()
    {
        var line = Line(W("Inbox", 100, 80, 90, 28), W("Squad", 260, 80, 90, 28), W("Transfers", 420, 80, 130, 28));
        var blocks = LayoutBuilder.Build(new[] { line }, Content, new TuningConfig { SplitGapFactor = 0.8 }, 0.78);
        Assert.Equal(3, blocks.Count);
        Assert.Single(blocks.Select(b => b.Group).Distinct());
        Assert.StartsWith("row-", blocks[0].Group);
    }

    [Fact]
    public void FontPx_IsLineHeightScaledTo1080_TimesFontScale()
    {
        // contentRect cao 1080 ⇒ hệ số 1; cao dòng 26 px × 0.78 ≈ 20
        var b = Assert.Single(LayoutBuilder.Build(new[] { Line(W("Board", 100, 200, 92, 26)) }, Content, T, 0.78));
        Assert.Equal((int)Math.Round(26 * 0.78), b.FontPx);
        // contentRect cao 2160 ⇒ quy về 1080: một nửa
        var big = new ContentRect(0, 0, 3840, 2160);
        var b2 = Assert.Single(LayoutBuilder.Build(new[] { Line(W("Board", 200, 400, 184, 52)) }, big, T, 0.78));
        Assert.Equal((int)Math.Round(52 * 0.5 * 0.78), b2.FontPx);
    }

    [Fact]
    public void SameColumn_WithJitteredOcrHeights_SharesOneGroupAndOneFontPx()
    {
        // hộp OCR cao 26 và 33 (nét lên/xuống) cùng mép trái 100 ⇒ cùng cột, cùng cỡ (trung vị)
        var lines = new[]
        {
            Line(W("Board", 100, 200, 92, 26)),
            Line(W("Transfer Committee", 100, 310, 300, 33)),
            Line(W("Scout", 100, 420, 90, 26)),
        };
        var blocks = LayoutBuilder.Build(lines, Content, T, 0.78);
        Assert.Equal(3, blocks.Count);
        Assert.Single(blocks.Select(b => b.Group).Distinct());
        Assert.Single(blocks.Select(b => b.FontPx).Distinct());
    }

    [Fact]
    public void Box01_IsNormalizedToContentRect()
    {
        var b = Assert.Single(LayoutBuilder.Build(new[] { Line(W("Board", 192, 108, 96, 27)) }, Content, T, 0.78));
        Assert.Equal(0.1, b.Box01.X, 6);
        Assert.Equal(0.1, b.Box01.Y, 6);
        Assert.Equal(0.05, b.Box01.W, 6);
    }
}

public class ItemIdTests
{
    [Fact]
    public void SameCell_SameText_SameId_EvenIfBboxShiftsAFewPx()
    {
        var a = ItemId.Compute(new Box(0.101, 0.2001, 0.05, 0.02), "Board");
        var b = ItemId.Compute(new Box(0.1015, 0.2004, 0.05, 0.02), "Board");
        Assert.Equal(a, b);
        Assert.Equal(12, a.Length);
    }

    [Fact]
    public void DifferentCellOrText_DifferentId()
    {
        var a = ItemId.Compute(new Box(0.101, 0.2001, 0.05, 0.02), "Board");
        Assert.NotEqual(a, ItemId.Compute(new Box(0.40, 0.2001, 0.05, 0.02), "Board"));
        Assert.NotEqual(a, ItemId.Compute(new Box(0.101, 0.2001, 0.05, 0.02), "Today"));
    }
}

public class ColorSamplerTests
{
    private static byte[] Fill(int w, int h, (byte r, byte g, byte b) bg)
    {
        var px = new byte[w * h * 4];
        for (int i = 0; i < px.Length; i += 4) { px[i] = bg.b; px[i + 1] = bg.g; px[i + 2] = bg.r; px[i + 3] = 255; }
        return px;
    }

    private static void Block(byte[] px, int w, int x0, int y0, int bw, int bh, (byte r, byte g, byte b) c)
    {
        for (int y = y0; y < y0 + bh; y++) for (int x = x0; x < x0 + bw; x++) { int i = (y * w + x) * 4; px[i] = c.b; px[i + 1] = c.g; px[i + 2] = c.r; }
    }

    [Fact]
    public void DarkBackground_WhiteText()
    {
        var px = Fill(400, 200, (20, 32, 44));
        Block(px, 400, 100, 80, 120, 14, (255, 255, 255)); // "chữ"
        var s = ColorSampler.Sample(px, 400, 200, 400 * 4, new Box(98, 78, 124, 18));
        Assert.Equal("#FF14202C", ColorSampler.Hex(s.Cover));
        Assert.Equal("#FFFFFFFF", ColorSampler.Hex(s.Text));
        Assert.True(s.RimSpread < 1);
    }

    [Fact]
    public void LightSelectionStripe_DarkText_CoverFollowsStripe()
    {
        var px = Fill(400, 200, (20, 32, 44));
        Block(px, 400, 40, 60, 320, 50, (200, 205, 210));   // vệt chọn sáng
        Block(px, 400, 100, 80, 120, 14, (30, 30, 30));     // chữ tối
        var s = ColorSampler.Sample(px, 400, 200, 400 * 4, new Box(98, 78, 124, 18));
        Assert.Equal("#FFC8CDD2", ColorSampler.Hex(s.Cover));
        Assert.Equal("#FF1E1E1E", ColorSampler.Hex(s.Text));
    }

    [Fact]
    public void GradientBackground_HasLargeRimSpread()
    {
        var px = Fill(400, 200, (0, 0, 0));
        for (int y = 0; y < 200; y++) for (int x = 0; x < 400; x++) { int i = (y * 400 + x) * 4; px[i] = px[i + 1] = px[i + 2] = (byte)(x * 255 / 399); }
        var s = ColorSampler.Sample(px, 400, 200, 400 * 4, new Box(100, 80, 200, 20));
        Assert.True(s.RimSpread > 18, $"spread={s.RimSpread}");
    }

    [Fact]
    public void RimIsClippedAtImageEdge_NoCrash()
    {
        var px = Fill(100, 50, (10, 10, 10));
        var s = ColorSampler.Sample(px, 100, 50, 400, new Box(0, 0, 50, 20));
        Assert.Equal("#FF0A0A0A", ColorSampler.Hex(s.Cover));
    }

    [Theory]
    [InlineData("auto", 30.0, "card")]
    [InlineData("auto", 5.0, "solid")]
    [InlineData("solid", 99.0, "solid")]
    [InlineData("card", 0.0, "card")]
    [InlineData("none", 99.0, "none")]
    public void CoverMode_Decision(string cfg, double spread, string expected)
        => Assert.Equal(expected, ColorSampler.DecideCoverMode(cfg, spread, 18));
}
