using Engine.Config;
using Engine.Geometry;
using Engine.Layout;
using Engine.Pipeline;
using Engine.Protocol;
using Engine.Translation;

namespace Engine.Tests;

public class RoomFinderTests
{
    private static byte[] Fill(int w, int h, byte g)
    {
        var px = new byte[w * h * 4];
        for (int i = 0; i < px.Length; i += 4) { px[i] = g; px[i + 1] = g; px[i + 2] = g; px[i + 3] = 255; }
        return px;
    }

    [Fact]
    public void Room_GrowsOverUniformBackground_UpToMaxFactor()
    {
        var px = Fill(400, 200, 30);
        var room = RoomFinder.Find(px, 400, 200, 400 * 4, new Box(150, 90, 100, 20), 0xFF1E1E1E, [], 2.0);
        Assert.True(room.W <= 200 + 1 && room.H <= 40 + 1);
        Assert.True(room.W > 150 && room.H > 20);
        Assert.True(room.X <= 150 && room.Y <= 90 && room.X + room.W >= 250 && room.Y + room.H >= 110); // ⊇ bbox
    }

    [Fact]
    public void Room_StopsAtOtherBoxAndAtDifferentBackground()
    {
        var px = Fill(400, 200, 30);
        for (int y = 0; y < 200; y++) for (int x = 300; x < 400; x++) { int i = (y * 400 + x) * 4; px[i] = px[i + 1] = px[i + 2] = 200; } // nền khác từ x=300
        var room = RoomFinder.Find(px, 400, 200, 400 * 4, new Box(150, 90, 100, 20), 0xFF1E1E1E, [new Box(60, 80, 80, 40)], 3.0);
        Assert.True(room.X >= 140, $"lấn bbox khác: x={room.X}");
        Assert.True(room.X + room.W <= 300, $"ra ngoài nền đồng nhất: right={room.X + room.W}");
    }
}

public class FramePipelineTests
{
    private sealed class FakeOcr : IOcrProvider
    {
        public int Calls;
        public Task<IReadOnlyList<OcrLineData>> RecognizeAsync(SourceFrame f, CancellationToken ct)
        {
            Calls++;
            OcrLineData L(string t, double x, double y, double w) => new(t, new Box(x, y, w, 26), [new OcrWordData(t, new Box(x, y, w, 26))]);
            IReadOnlyList<OcrLineData> r = f.Name switch
            {
                "A" => [L("Scout Report", 100, 200, 200), L("Totally unknown sentence here", 100, 300, 300)],
                "B" => [L("Inbox", 300, 100, 100)],
                _ => [],
            };
            return Task.FromResult(r);
        }
    }

    private sealed class CaptureSink : IFrameSink
    {
        public List<OverlayFrame> Frames { get; } = new();
        public void Send(string line) => Frames.Add(ProtocolJson.ParseFrame(line)!);
    }

    private static SourceFrame Img(string name, long t)
    {
        const int w = 640, h = 360;
        var px = new byte[w * h * 4];
        for (int i = 0; i < px.Length; i += 4) { px[i] = 40; px[i + 1] = 40; px[i + 2] = 40; px[i + 3] = 255; }
        var (bx, by) = name == "A" ? (100, 200) : (300, 100);
        for (int y = by; y < by + 26; y++) for (int x = bx; x < bx + 200; x++) { int i = (y * w + x) * 4; px[i] = px[i + 1] = px[i + 2] = 230; }
        return new SourceFrame(px, w, h, t, name);
    }

    private static (FramePipeline p, CaptureSink s, FakeOcr o, TranslationService svc) Make()
    {
        var cfg = new EngineConfig();
        var home = Directory.CreateTempSubdirectory("vietsub-test-").FullName;
        var g = new Glossary { Version = 1, Terms = [new GlossaryTerm("Scout Report", "Báo cáo tuyển trạch", "Báo cáo", "ui"), new GlossaryTerm("Inbox", "Hộp thư", null, "ui")] };
        var cache = TranslationCache.Load(null, Path.Combine(home, "c.json"));
        var usage = new UsageCounter(Path.Combine(home, "u"), 100000, () => new DateOnly(2026, 11, 18));
        var svc = new TranslationService(g, cache, null, usage, () => DateTime.UtcNow, cfg.Tuning);
        var sink = new CaptureSink(); var ocr = new FakeOcr();
        return (new FramePipeline(cfg, ocr, svc, sink, "sess", null), sink, ocr, svc);
    }

    [Fact]
    public async Task StableScreen_SendsTranslatedItemsOnce_ThenHeartbeats_AndDoesNotDrawUntranslated()
    {
        var (p, s, ocr, _) = Make();
        for (long t = 0; t <= 3000; t += 250) await p.OnFrameAsync(Img("A", t), default);

        var withItems = s.Frames.Where(f => f.Items.Count > 0).ToList();
        Assert.NotEmpty(withItems);
        var first = withItems[0];
        Assert.Single(first.Items);                                  // chỉ câu có glossary; câu lạ KHÔNG vẽ (D5)
        Assert.Equal("Báo cáo tuyển trạch", first.Items[0].Text);
        Assert.Equal("Báo cáo", first.Items[0].Alt);
        Assert.Equal(1, ocr.Calls);                                  // cùng chữ ký ⇒ không OCR lại
        Assert.True(withItems.Count >= 3, "phải có nhịp tim gửi lại gói cũ");
        Assert.All(withItems, f => Assert.Equal(first.Items[0].Id, f.Items[0].Id));
        Assert.Equal(withItems.OrderBy(f => f.Seq).Select(f => f.Seq), withItems.Select(f => f.Seq)); // seq tăng
        Assert.Equal(withItems.Select(f => f.Seq).Distinct().Count(), withItems.Count);
        Assert.Equal(1, p.ItemSetChanges);                           // 0 lần đổi chữ khi khung đứng yên
        Assert.Contains(p.LastMissing, m => m.Key == "Totally unknown sentence here");
        Assert.Equal(3000, first.TtlMs);
        Assert.Equal(24, first.Display!.MinFontPx);
    }

    [Fact]
    public async Task ChangingScreen_SendsEmptyImmediately_ThenNewItemsWhenStable()
    {
        var (p, s, _, _) = Make();
        for (long t = 0; t <= 1000; t += 250) await p.OnFrameAsync(Img("A", t), default);
        int before = s.Frames.Count;
        await p.OnFrameAsync(Img("B", 1250), default);               // cuộn/ đổi màn
        Assert.True(s.Frames.Count > before);
        Assert.Empty(s.Frames[^1].Items);                            // gói items:[] gửi NGAY
        for (long t = 1500; t <= 2500; t += 250) await p.OnFrameAsync(Img("B", t), default);
        var last = s.Frames.Last(f => f.Items.Count > 0);
        Assert.Equal("Hộp thư", last.Items[0].Text);
        Assert.True(p.ItemSetChanges >= 3); // bộ A, bộ rỗng, bộ B
    }

    [Fact]
    public async Task Pattern_HasCornersAndFitsLimits()
    {
        var f = Commands3.BuildPattern(new EngineConfig(), "s", 1);
        Assert.True(f.Items.Count is > 4 and <= 200);
        Assert.Contains(f.Items, i => i.Id == "p-tl" && i.Box.X == 0 && i.Box.Y == 0);
        Assert.Contains(f.Items, i => i.Id == "p-br" && Math.Abs(i.Box.X + i.Box.W - 1) < 1e-9 && Math.Abs(i.Box.Y + i.Box.H - 1) < 1e-9);
        Assert.All(f.Items, i => Assert.Matches("^#[0-9A-F]{8}$", i.TextColor));
        await Task.CompletedTask;
    }
}
