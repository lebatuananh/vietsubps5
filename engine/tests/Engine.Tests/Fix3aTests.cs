using System.Text.Json;
using Engine.Config;
using Engine.Geometry;
using Engine.Layout;
using Engine.Pipeline;
using Engine.Protocol;
using Engine.Translation;

namespace Engine.Tests;

// Fix round 3a (Rev 1.3): B1–B4 + NB. Mọi transport/ OCR/ khung là giả, VIETSUB_HOME = thư mục tạm; 0 HTTP.
internal static class Fx
{
    public static string Temp() => Directory.CreateTempSubdirectory("vietsub-fix3a-").FullName;

    public static Glossary G => new()
    {
        Version = 1,
        Terms = [new GlossaryTerm("Board", "Ban lãnh đạo", null, null), new GlossaryTerm("Today", "Hôm nay", null, null)],
    };

    public static SourceFrame Solid(int w, int h, byte v)
    {
        var p = new byte[w * h * 4];
        for (int i = 0; i < p.Length; i += 4) { p[i] = p[i + 1] = p[i + 2] = v; p[i + 3] = 255; }
        return new SourceFrame(p, w, h, 0, null);
    }

    public static OcrLineData Line(string text, double x, double y, double w, double h)
    {
        var parts = text.Split(' '); double cw = w / Math.Max(1, text.Length); double cx = x; var words = new List<OcrWordData>();
        foreach (var p in parts) { words.Add(new OcrWordData(p, new Box(cx, y, p.Length * cw, h))); cx += (p.Length + 1) * cw; }
        return new OcrLineData(text, new Box(x, y, w, h), words);
    }

    public static TranslationService Svc(EngineConfig cfg, ITranslationBackend? b = null, UsageCounter? u = null, TranslationCache? c = null)
    {
        var home = Temp();
        return new TranslationService(G, c ?? TranslationCache.Load(null, Path.Combine(home, "c.json")), b,
            u ?? new UsageCounter(Path.Combine(home, "u"), 100000, () => new DateOnly(2026, 11, 18)), () => new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc), cfg.Tuning);
    }

    public sealed class FakeOcr(IReadOnlyList<OcrLineData> lines) : IOcrProvider
    {
        public int Calls;
        public Task<IReadOnlyList<OcrLineData>> RecognizeAsync(SourceFrame frame, CancellationToken ct) { Calls++; return Task.FromResult(lines); }
    }

    public sealed class RecSink : IFrameSink
    {
        public int Count, LastItems; public OverlayFrame? Last;
        public void Send(string line) { Count++; Last = ProtocolJson.ParseFrame(line)!; LastItems = Last.Items.Count; }
    }
}

public class B1_ItemIdUniqueTests
{
    [Fact]
    public void SameCellSameText_DifferentOccurrence_DifferentId_AndStable()
    {
        var box = new Box(0.10, 0.40, 0.05, 0.02);
        var a0 = ItemId.Compute(box, "Board", 0);
        var a1 = ItemId.Compute(box, "Board", 1);
        Assert.NotEqual(a0, a1);
        Assert.Equal(a0, ItemId.Compute(box, "Board", 0));
        Assert.Equal(a1, ItemId.Compute(box, "Board", 1));
    }

    [Fact]
    public void TwoIdenticalLabels_InAdjacentRows_GetDistinctIds_StableAcrossFrames()
    {
        var cfg = new EngineConfig();
        var content = new ContentRect(0, 0, 1920, 1080);
        var svc = Fx.Svc(cfg);
        var frame = Fx.Solid(1920, 1080, 30);
        // y=0.367 và 0.441 cùng ô lưới hàng 3 (chuỗi review 3a: "Ban lãnh đạo" ×2)
        var lines = new[] { Fx.Line("Board", 100, 396, 90, 28), Fx.Line("Board", 100, 476, 90, 28), Fx.Line("Today", 900, 396, 90, 28), Fx.Line("Today", 900, 476, 90, 28) };
        IReadOnlyList<string> Ids()
        {
            var blocks = LayoutBuilder.Build(lines, content, cfg.Tuning, cfg.Display.FontScale);
            return ItemBuilder.Build(frame, content, blocks, svc, cfg).Items.Select(i => i.Id).ToList();
        }
        var ids1 = Ids();
        Assert.Equal(4, ids1.Count);
        Assert.Equal(4, ids1.Distinct().Count());       // duy nhất trong một gói
        Assert.Equal(ids1, Ids());                       // ổn định giữa các khung
    }
}

public class B2_FatalApiErrorTests
{
    private sealed class CountingTransport(Func<ClaudeRequest, ClaudeRawResponse> f) : IClaudeTransport
    {
        public int Calls;
        public Task<ClaudeRawResponse> SendAsync(ClaudeRequest r, CancellationToken ct) { Interlocked.Increment(ref Calls); return Task.FromResult(f(r)); }
    }

    private static List<NormalizedSource> Keys40() => Enumerable.Range(1, 40)
        .Select(i => SourceNormalizer.Normalize($"Your scout has found a promising young player called P{i}x Q{i * 7}y")).ToList();

    [Fact]
    public async Task Http401_DisablesBackendForTheSession_OneRequest_NoAttempts_CharsRefunded()
    {
        var cfg = new EngineConfig();
        var home = Fx.Temp();
        var cache = TranslationCache.Load(null, Path.Combine(home, "c.json"));
        var usage = new UsageCounter(Path.Combine(home, "u"), 100000, () => new DateOnly(2026, 11, 18));
        var tr = new CountingTransport(_ => throw new ClaudeTransportException(false, "API: 401 invalid x-api-key"));
        var logs = new List<string>();
        var svc = new TranslationService(Fx.G, cache, new ClaudeBackend(tr, Fx.G), usage, () => DateTime.UtcNow, cfg.Tuning, log: logs.Add);
        var keys = Keys40();

        Assert.Equal(0, await svc.ProcessAsync(keys, default));
        Assert.Equal(1, tr.Calls);                                   // không tách đôi: đúng 1 HTTP
        Assert.True(svc.BackendDisabled);
        Assert.Contains("401", svc.DisabledReason);
        Assert.Equal(0, usage.Used);                                 // ký tự đã giữ chỗ được hoàn lại
        Assert.DoesNotContain(cache.All, e => e.Status == "rejected"); // 0 entry rejected, attempts không đổi
        Assert.Equal(1, logs.Count(l => l.Contains("tắt backend")));   // log đúng một dòng

        Assert.Equal(0, await svc.ProcessAsync(keys, default));       // lần 2 cùng phiên
        Assert.Equal(1, tr.Calls);
        Assert.Equal(1, logs.Count(l => l.Contains("tắt backend")));
    }

    [Fact]
    public async Task Fatal_DoesNotPoisonNextSession_AfterKeyFixed()
    {
        var cfg = new EngineConfig();
        var home = Fx.Temp();
        var cache = TranslationCache.Load(null, Path.Combine(home, "c.json"));
        var usage = new UsageCounter(Path.Combine(home, "u"), 100000, () => new DateOnly(2026, 11, 18));
        var keys = Keys40();
        var bad = new TranslationService(Fx.G, cache, new ClaudeBackend(new CountingTransport(_ => throw new ClaudeTransportException(false, "401")), Fx.G), usage, () => DateTime.UtcNow, cfg.Tuning);
        await bad.ProcessAsync(keys, default);
        var good = new CountingTransport(r =>
        {
            using var doc = JsonDocument.Parse(r.User);
            var items = doc.RootElement.EnumerateArray().Select(e => new { id = e.GetProperty("id").GetString(), vi = "VI " + e.GetProperty("en").GetString() });
            return new ClaudeRawResponse(JsonSerializer.Serialize(new { items }), "end_turn", 1, 1, 0, 0);
        });
        var svc2 = new TranslationService(Fx.G, cache, new ClaudeBackend(good, Fx.G), usage, () => DateTime.UtcNow, cfg.Tuning);
        Assert.Equal(40, await svc2.ProcessAsync(keys, default));
        Assert.Equal(1, good.Calls);
    }

    [Fact]
    public async Task ClaudeBackend_NonTransientTransportError_IsFatal()
    {
        var b = new ClaudeBackend(new CountingTransport(_ => throw new ClaudeTransportException(false, "400")), Fx.G);
        var r = await b.TranslateAsync([new BackendItem("s1", "Hello world")], default);
        Assert.Equal(BackendOutcome.Fatal, r.Outcome);
    }

    [Theory]
    [InlineData(400, false, false)]
    [InlineData(401, false, false)]
    [InlineData(403, false, false)]
    [InlineData(404, false, false)]
    [InlineData(408, true, false)]
    [InlineData(409, true, false)]
    [InlineData(429, true, true)]
    [InlineData(500, true, true)]
    [InlineData(529, true, true)]
    public void StatusClassification(int status, bool transient, bool refundable)
    {
        var (t, r) = AnthropicSdkTransport.Classify(status);
        Assert.Equal(transient, t);
        Assert.Equal(refundable, r);
    }
}

public class NB3_NB4_UsageTests
{
    private static NormalizedSource N(string k) => new(k, []);

    [Fact]
    public async Task Transient429_RefundsChars_TimeoutKeepsThem()
    {
        var cfg = new EngineConfig();
        var u1 = new UsageCounter(Fx.Temp(), 100000, () => new DateOnly(2026, 11, 18));
        var s1 = Fx.Svc(cfg, new FakeBackend(_ => new(BackendOutcome.Transient, [], "429", Refund: true)), u1);
        await s1.ProcessAsync([N("Hello there world")], default);
        Assert.Equal(0, u1.Used);

        var u2 = new UsageCounter(Fx.Temp(), 100000, () => new DateOnly(2026, 11, 18));
        var s2 = Fx.Svc(cfg, new FakeBackend(_ => new(BackendOutcome.Transient, [], "timeout")), u2);
        await s2.ProcessAsync([N("Hello there world")], default);
        Assert.Equal("Hello there world".Length, u2.Used);
    }

    [Fact]
    public void Usage_WritesAtomically_NoTmpLeft_AndRefundWorks()
    {
        var dir = Fx.Temp();
        var u = new UsageCounter(dir, 1000, () => new DateOnly(2026, 11, 18));
        Assert.True(u.TryReserve(100));
        u.Refund(40);
        Assert.Equal(60, u.Used);
        Assert.Empty(Directory.GetFiles(dir, "*.tmp"));
        Assert.Equal(60, new UsageCounter(dir, 1000, () => new DateOnly(2026, 11, 18)).Used);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{\"chars\":")]
    [InlineData("[1]")]
    [InlineData("{\"chars\":\"x\"}")]
    [InlineData("{\"chars\":-5}")]
    public void CorruptUsageFile_IsFailClosed_NoThrow(string content)
    {
        var dir = Fx.Temp();
        File.WriteAllText(Path.Combine(dir, "2026-11-18.json"), content);
        var u = new UsageCounter(dir, 1000, () => new DateOnly(2026, 11, 18));
        Assert.True(u.CapReached);          // coi như đã chạm trần
        Assert.False(u.TryReserve(1));
        Assert.True(u.TakeCapWarning());
    }
}

public class NB1_NB5_NB6_TranslationTests
{
    private static NormalizedSource N(string k) => new(k, []);

    [Fact]
    public async Task RejectedSentence_IsNotRetriedInTheSameSession()
    {
        var cfg = new EngineConfig();
        var cache = TranslationCache.Load(null, Path.Combine(Fx.Temp(), "c.json"));
        var bad = new FakeBackend(items => new(BackendOutcome.Ok, items.Select(i => new BackendTranslated(i.Id, "sai slot {N9}", null)).ToList(), null));
        var s = Fx.Svc(cfg, bad, c: cache);
        await s.ProcessAsync([N("Budget is fine today")], default);
        await s.ProcessAsync([N("Budget is fine today")], default);
        Assert.Equal(1, bad.Calls);
        Assert.Equal(1, cache.Get("fc26", "Budget is fine today")!.Attempts);
        var s2 = Fx.Svc(cfg, bad, c: cache);                       // phiên sau mới được lần 2
        await s2.ProcessAsync([N("Budget is fine today")], default);
        Assert.Equal(2, cache.Get("fc26", "Budget is fine today")!.Attempts);
    }

    [Fact]
    public async Task FakeTranslator_DoesNotWriteUserCache_NorCountUsage_ButServesInMemory()
    {
        var cfg = new EngineConfig();
        var home = Fx.Temp();
        var path = Path.Combine(home, "c.json");
        var cache = TranslationCache.Load(null, path);
        var usage = new UsageCounter(Path.Combine(home, "u"), 100000, () => new DateOnly(2026, 11, 18));
        var s = Fx.Svc(cfg, new FakeTranslator(), usage, cache);
        Assert.Equal(1, await s.ProcessAsync([N("Club Secretary says hello")], default));
        Assert.Equal("[VI] Club Secretary says hello", s.Lookup("Club Secretary says hello")!.Text);
        Assert.Equal(0, usage.Used);
        cache.Save();
        Assert.DoesNotContain("Club Secretary", File.Exists(path) ? File.ReadAllText(path) : "");
        Assert.True(TranslationCache.Load(null, path).Get("fc26", "Club Secretary says hello") is null);
    }

    [Fact]
    public void ConcurrentSaves_DoNotThrow()
    {
        var cache = TranslationCache.Load(null, Path.Combine(Fx.Temp(), "c.json"));
        for (int i = 0; i < 50; i++) cache.Put(new TranslationEntry { SourceKey = "k" + i, Target = "v" });
        var errors = new List<Exception>();
        Parallel.For(0, 200, _ => { try { cache.Save(); } catch (Exception e) { lock (errors) errors.Add(e); } });
        Assert.Empty(errors);
    }
}

public class B3_DynamicBackgroundUnderItemTests
{
    // Chuỗi khung của Reviewer (case J/K): 10 s, 4 khung/s; nền ô đổi 60↔110 mỗi 250 ms.
    private static async Task<(int transitions, long maxGap, int ocr, int sent, int ttl)> Run(bool animateItemCell)
    {
        var cfg = new EngineConfig();
        var sink = new Fx.RecSink();
        var ocr = new Fx.FakeOcr([Fx.Line("Board", 20, 20, 90, 28)]);
        var pipe = new FramePipeline(cfg, ocr, Fx.Svc(cfg), sink, "s");
        var basePx = Fx.Solid(1920, 1080, 30).Bgra;
        int transitions = 0; bool prev = false; long lastWith = -1, maxGap = 0;
        for (long t = 0; t <= 10000; t += 250)
        {
            var px = (byte[])basePx.Clone();
            byte v = (byte)((t / 250) % 2 == 0 ? 60 : 110);
            int cx0 = animateItemCell ? 0 : 1800, cy0 = animateItemCell ? 0 : 960;
            for (int y = cy0; y < cy0 + 120; y++)
                for (int x = cx0; x < cx0 + 120; x++)
                {
                    if (animateItemCell && x >= 20 && x < 110 && y >= 20 && y < 48) continue; // chữ đứng yên
                    int i = (y * 1920 + x) * 4; px[i] = px[i + 1] = px[i + 2] = v;
                }
            int before = sink.Count;
            await pipe.OnFrameAsync(new SourceFrame(px, 1920, 1080, t, null), default);
            bool has = sink.LastItems > 0;
            if (sink.Count > before)
            {
                if (has != prev) transitions++;
                prev = has;
                if (has) { if (lastWith >= 0) maxGap = Math.Max(maxGap, t - lastWith); lastWith = t; }
            }
            else if (prev && lastWith >= 0) maxGap = Math.Max(maxGap, t - lastWith);
        }
        return (transitions, maxGap, ocr.Calls, sink.Count, cfg.Tuning.TtlMs);
    }

    [Fact]
    public async Task CaseJ_AnimatedBackgroundAroundStillText_AtMostOneChangeIn10s()
    {
        var r = await Run(animateItemCell: true);
        Assert.True(r.transitions <= 1, $"item hiện↔mất {r.transitions} lần (OCR {r.ocr})");
    }

    [Fact]
    public async Task CaseK_AnimationInAnotherCell_StillFine()
    {
        var r = await Run(animateItemCell: false);
        Assert.True(r.transitions <= 1, $"item hiện↔mất {r.transitions} lần");
        Assert.True(r.maxGap < r.ttl, $"khe gói {r.maxGap} ms ≥ ttl {r.ttl}");
    }

    [Fact]
    public async Task CaseJ2_TextItselfChanging_StillRemovesItemImmediately()
    {
        // nền động quanh chữ NHƯNG chữ thật sự đổi (đổi trang) ⇒ item phải bị gỡ, không bị "bảo vệ" mù
        var cfg = new EngineConfig();
        var sink = new Fx.RecSink();
        var ocr = new Fx.FakeOcr([Fx.Line("Board", 20, 20, 90, 28)]);
        var pipe = new FramePipeline(cfg, ocr, Fx.Svc(cfg), sink, "s");
        byte[] Frame(long t, bool textChanged)
        {
            var px = (byte[])Fx.Solid(1920, 1080, 30).Bgra.Clone();
            byte v = (byte)((t / 250) % 2 == 0 ? 60 : 110);
            for (int y = 0; y < 120; y++)
                for (int x = 0; x < 120; x++)
                {
                    bool inText = x >= 20 && x < 110 && y >= 20 && y < 48;
                    int i = (y * 1920 + x) * 4;
                    byte g = inText ? (byte)(textChanged ? 220 : 30) : v;
                    px[i] = px[i + 1] = px[i + 2] = g;
                }
            return px;
        }
        long t = 0;
        for (; t <= 8000; t += 250) await pipe.OnFrameAsync(new SourceFrame(Frame(t, false), 1920, 1080, t, null), default);
        Assert.True(sink.LastItems > 0, "item phải đã hiện trước khi đổi chữ");
        await pipe.OnFrameAsync(new SourceFrame(Frame(t, true), 1920, 1080, t, null), default);
        Assert.Equal(0, sink.LastItems);
    }
}

public class NB7_MatchModeTests
{
    private static byte[] Sig(byte v) => Enumerable.Repeat(v, FrameSignature.Cells).Select(x => x).ToArray();

    [Fact]
    public void SingleFullScreenPageChange_IsNotMatchMode()
    {
        var m = new MotionTracker(new TuningConfig());
        m.Update(0, Sig(30));
        var u = m.Update(250, Sig(120));          // đổi trang toàn màn đúng một nhịp
        Assert.False(u.MatchMode);
        MotionUpdate last = u;
        for (long t = 500; t <= 1000; t += 250) last = m.Update(t, Sig(120));
        Assert.Equal(MotionState.Stable, last.State);   // đứng yên sau StableMs (250), không phải chờ 1,5 s
    }

    [Fact]
    public void SustainedMotion_3BeatsIn3s_IsMatchMode()
    {
        var m = new MotionTracker(new TuningConfig());
        m.Update(0, Sig(30));
        bool match = false;
        for (int i = 1; i <= 4; i++) match |= m.Update(i * 250, Sig((byte)(30 + i * 30))).MatchMode;
        Assert.True(match);
    }
}

public class NB8_NB9_NB10_LayoutTests
{
    [Fact]
    public void Email_TwoLinesWithOcrHeightJitter_AreMerged()
    {
        var cfg = new EngineConfig();
        var lines = new[]
        {
            Fx.Line("Your contract offer has been", 300, 500, 520, 23),
            Fx.Line("accepted.", 300, 530, 130, 29),   // 20,7% lệch (review 3a: NB8)
        };
        var blocks = LayoutBuilder.Build(lines, new ContentRect(0, 0, 1920, 1080), cfg.Tuning, cfg.Display.FontScale);
        Assert.Single(blocks);
        Assert.Equal("Your contract offer has been accepted.", blocks[0].Text);
    }

    [Fact]
    public void FontScale_DefaultIsOne()
    {
        Assert.Equal(1.0, new EngineConfig().Display.FontScale);
    }

    [Fact]
    public async Task Frame_IsClampedTo200Items_AndText1000Chars()
    {
        var cfg = new EngineConfig();
        var cache = TranslationCache.Load(null, Path.Combine(Fx.Temp(), "c.json"));
        var lines = new List<OcrLineData>();
        for (int i = 0; i < 280; i++)
        {
            string txt = $"Player name entry {(char)('a' + i % 26)}{(char)('a' + i / 26)}";
            cache.Put(new TranslationEntry { SourceKey = SourceNormalizer.Normalize(txt).Key, Target = i == 0 ? new string('x', 1500) : "Tên " + i });
            lines.Add(Fx.Line(txt, 10 + (i % 8) * 238, 10 + (i / 8) * 30, 200, 14));
        }
        var sink = new Fx.RecSink();
        var pipe = new FramePipeline(cfg, new Fx.FakeOcr(lines), Fx.Svc(cfg, c: cache), sink, "s");
        for (long t = 0; t <= 1000; t += 250) await pipe.OnFrameAsync(Fx.Solid(1920, 1080, 30) with { TimestampMs = t }, default);
        Assert.InRange(sink.LastItems, 1, 200);
        Assert.All(sink.Last!.Items, i => Assert.True(i.Text.Length <= 1000));
    }
}

public class NB11_NB12_CliConfigTests
{
    [Fact]
    public void BareBooleanFlag_IsAccepted()
    {
        var a = Args.Parse(["run", "--no-send", "--verbose"], 1);
        Assert.Equal("true", a.Get("no-send"));
        Assert.Equal("true", a.Get("verbose"));
        var b = Args.Parse(["run", "--no-send", "true", "--translator", "cache"], 1);
        Assert.Equal("true", b.Get("no-send"));
        Assert.Equal("cache", b.Get("translator"));
    }

    [Fact]
    public void NonBooleanFlagWithoutValue_StillFails()
        => Assert.Throws<ArgumentException>(() => Args.Parse(["pair", "--tv"], 1));

    [Theory]
    [InlineData("auto")]
    [InlineData("solid")]
    [InlineData("CARD")]
    [InlineData("none")]
    public void CoverMode_Valid(string m) { new EngineConfig { Display = { CoverMode = m } }.Validate(); }

    [Fact]
    public void CoverMode_Unknown_IsRejectedAtLoad()
        => Assert.Throws<ArgumentException>(() => new EngineConfig { Display = { CoverMode = "foo" } }.Validate());
}

public class Sliver_C1_OriginalPixelsMustNotPeekOutTests
{
    // Review 3b/Dev: OCR bbox ngắn hơn chữ thật ~2 px ⇒ room ôm sát bbox ⇒ TV cắt vùng che trong room ⇒ sót vệt chữ gốc.
    [Fact]
    public void OcrBoxTwoPxShort_RoomAndBoxStillCoverAllInk()
    {
        var cfg = new EngineConfig();
        const int W = 1920, H = 1080;
        var f = Fx.Solid(W, H, 30);
        // chữ thật chiếm x 500..702, y 530..558; OCR báo 500..700 × 532..556 (hụt 2 px mỗi phía)
        for (int y = 530; y < 558; y++) for (int x = 500; x < 702; x++) { int i = (y * W + x) * 4; f.Bgra[i] = f.Bgra[i + 1] = f.Bgra[i + 2] = 230; }
        var content = new ContentRect(0, 0, W, H);
        var lines = new[] { Fx.Line("Board", 500, 532, 200, 24) };
        var blocks = LayoutBuilder.Build(lines, content, cfg.Tuning, cfg.Display.FontScale);
        var item = ItemBuilder.Build(f, content, blocks, Fx.Svc(cfg), cfg).Items.Single();

        // vùng che TV (README): hợp(box, chữ) + lề ≥ 6/1080·H, cắt trong room. Chữ Việt có thể hẹp hơn ⇒ lấy riêng box.
        double margin = 6.0 * H / 1080;
        var room = item.Room!.Value;
        double l = Math.Max(item.Box.X * W - margin, room.X * W), r = Math.Min((item.Box.X + item.Box.W) * W + margin, (room.X + room.W) * W);
        double t = Math.Max(item.Box.Y * H - margin, room.Y * H), b = Math.Min((item.Box.Y + item.Box.H) * H + margin, (room.Y + room.H) * H);
        Assert.True(l <= 500 && r >= 702, $"cover x {l:0.#}..{r:0.#} không phủ mực 500..702");
        Assert.True(t <= 530 && b >= 558, $"cover y {t:0.#}..{b:0.#} không phủ mực 530..558");
    }
}
