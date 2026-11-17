using Engine.Config;
using Engine.Geometry;
using Engine.Layout;
using Engine.Pipeline;

namespace Engine.Tests;

// Fix round 3b (Rev 1.4): nhãn một dòng gửi maxLines = 1; đoạn văn giữ số dòng gốc + 2.
public class Fix3b_MaxLinesTests
{
    private static readonly ContentRect Content = new(0, 0, 1920, 1080);

    [Fact]
    public void SingleLineLabels_GetMaxLines1()
    {
        var cfg = new EngineConfig();
        var lines = new[] { Fx.Line("Board", 100, 396, 90, 28), Fx.Line("Cap nhat muc tieu mua giai", 600, 396, 380, 28), Fx.Line("Today", 1500, 396, 90, 28) };
        var blocks = LayoutBuilder.Build(lines, Content, cfg.Tuning, cfg.Display.FontScale);
        Assert.NotEmpty(blocks);
        Assert.All(blocks, b => Assert.Equal(1, b.MaxLines));
    }

    [Fact]
    public void Paragraph_KeepsOriginalLineCountPlus2()
    {
        var cfg = new EngineConfig();
        var lines = new[]
        {
            Fx.Line("Your contract offer has been", 300, 500, 520, 28),
            Fx.Line("accepted by the player and the", 300, 532, 540, 28),
            Fx.Line("board is delighted.", 300, 564, 340, 28),
        };
        var b = Assert.Single(LayoutBuilder.Build(lines, Content, cfg.Tuning, cfg.Display.FontScale));
        Assert.Equal(3, b.LineCount);
        Assert.Equal(5, b.MaxLines);
    }

    [Fact]
    public void ItemsInFrame_CarryTheSameMaxLines()
    {
        var cfg = new EngineConfig();
        var frame = Fx.Solid(1920, 1080, 30);
        var blocks = LayoutBuilder.Build([Fx.Line("Board", 100, 396, 90, 28)], Content, cfg.Tuning, cfg.Display.FontScale);
        var it = Assert.Single(ItemBuilder.Build(frame, Content, blocks, Fx.Svc(cfg), cfg).Items);
        Assert.Equal(1, it.MaxLines);
    }
}
