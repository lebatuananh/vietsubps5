using Engine.Config;
using Engine.Translation;

namespace Engine.Tests;

public class SourceNormalizerTests
{
    [Fact]
    public void MasksMoney_AndKeepsOriginalSlotValue()
    {
        var n = SourceNormalizer.Normalize("Transfer Budget: €45.0M");
        Assert.Equal("Transfer Budget: {N1}", n.Key);
        Assert.Equal(new[] { "€45.0M" }, n.Slots);
    }

    [Fact]
    public void MasksMultipleSlots_InOrderOfAppearance()
    {
        var n = SourceNormalizer.Normalize("Contract: 3 years, £40K/wk, OVR 84, 12 Nov 2027 at 18:30 (95%)");
        Assert.Contains("{N1}", n.Key);
        Assert.DoesNotContain("40K", n.Key);
        Assert.DoesNotContain("84", n.Key);
        Assert.DoesNotContain("18:30", n.Key);
        Assert.DoesNotContain("95%", n.Key);
        Assert.Equal(n.Slots.Count, System.Text.RegularExpressions.Regex.Matches(n.Key, @"\{N\d+\}").Count);
        Assert.Equal("3", n.Slots[0]);
    }

    [Fact]
    public void NormalizesWhitespaceAndQuotes()
    {
        Assert.Equal("Don't miss it", SourceNormalizer.Normalize("  Don’t   miss\tit ").Key);
    }

    [Fact]
    public void SameSentenceDifferentNumbers_SameKey()
    {
        Assert.Equal(SourceNormalizer.Normalize("Offer of €12.5M accepted").Key, SourceNormalizer.Normalize("Offer of €3.1M accepted").Key);
    }

    [Fact]
    public void Restore_PutsOriginalValuesBack()
    {
        var n = SourceNormalizer.Normalize("Transfer Budget: €45.0M");
        Assert.Equal("Ngân sách chuyển nhượng: €45.0M", SourceNormalizer.Restore("Ngân sách chuyển nhượng: {N1}", n.Slots));
    }

    [Fact]
    public void NoVariables_NoSlots()
    {
        var n = SourceNormalizer.Normalize("Scout Report");
        Assert.Equal("Scout Report", n.Key);
        Assert.Empty(n.Slots);
    }
}

public class SlotValidatorTests
{
    [Theory]
    [InlineData("Budget: {N1}", "Ngân sách: {N1}", true)]
    [InlineData("A {N1} B {N2}", "B {N2} A {N1}", true)]      // đổi thứ tự được
    [InlineData("A {N1} B {N2}", "A {N1}", false)]            // thiếu
    [InlineData("A {N1}", "A {N1} {N2}", false)]              // thừa
    [InlineData("A {N1}", "A {N2}", false)]                   // sai chỉ số
    [InlineData("Scout Report", "Báo cáo tuyển trạch", true)] // không slot
    [InlineData("Scout Report", "Báo cáo {N1}", false)]
    public void Validate(string src, string vi, bool ok) => Assert.Equal(ok, SlotValidator.Validate(src, vi));
}

public class LlmEligibilityTests
{
    [Theory]
    [InlineData("INBOX", false)]                 // ≤ 4 ký tự... 5 ký tự nhưng chữ hoa ≤ 3 từ
    [InlineData("TRANSFER HUB", false)]          // chữ hoa ≤ 3 từ
    [InlineData("ACCEPT", false)]
    [InlineData("Yes", false)]                   // ≤ 4 ký tự
    [InlineData("OK", false)]
    [InlineData("Scout Report", true)]
    [InlineData("Your contract offer has been accepted", true)]
    [InlineData("THE BOARD HAS DECIDED TO SACK YOU NOW", true)] // chữ hoa nhưng > 3 từ
    public void Eligible(string key, bool expected) => Assert.Equal(expected, LlmEligibility.IsEligible(key));
}

public class DataFilesTests
{
    private static string DataFile(string name)
    {
        var d = Path.Combine(AppContext.BaseDirectory, "data", "fc26", name);
        return File.Exists(d) ? d : throw new FileNotFoundException(d);
    }

    [Fact]
    public void Glossary_KeysUnique_ShortViShorter_NonEmpty()
    {
        var g = Glossary.Load(DataFile("glossary.json"));
        Assert.True(g.Version >= 1);
        Assert.True(g.Terms.Count >= 60, $"chỉ {g.Terms.Count} mục");
        var dup = g.Terms.GroupBy(x => x.TermEn.Trim().ToLowerInvariant()).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        Assert.Empty(dup);
        foreach (var t in g.Terms)
        {
            Assert.False(string.IsNullOrWhiteSpace(t.TermEn));
            Assert.False(string.IsNullOrWhiteSpace(t.TermVi));
            if (t.ShortVi is not null) Assert.True(t.ShortVi.Length < t.TermVi.Length, $"short_vi không ngắn hơn: {t.TermEn}");
        }
    }

    [Fact]
    public void SeedCache_ValidSlots_MachineStatus_UniqueKeys_GlossaryDoesNotContradict()
    {
        var cache = TranslationCache.Load(DataFile("seed-cache.json"), Path.Combine(Directory.CreateTempSubdirectory().FullName, "none.json"));
        Assert.True(cache.All.Count >= 40, $"seed chỉ {cache.All.Count}");
        foreach (var e in cache.All)
        {
            Assert.True(SlotValidator.Validate(e.SourceKey, e.Target), $"slot sai: {e.SourceKey}");
            Assert.Equal("machine", e.Status);
            Assert.False(string.IsNullOrWhiteSpace(e.Target));
        }
        var keys = cache.All.Select(e => e.SourceKey.ToLowerInvariant()).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        var g = Glossary.Load(DataFile("glossary.json"));
        var both = cache.All.Where(e => g.Lookup(e.SourceKey) is not null).Select(e => e.SourceKey).ToList();
        Assert.Empty(both); // glossary và seed không trùng khoá (một nguồn chân lý cho mỗi câu)
    }
}

public class TranslationCacheTests
{
    private static string Temp() => Directory.CreateTempSubdirectory("vietsub-test-").FullName;

    private static TranslationEntry E(string key, string vi, string status = "machine") => new() { SourceKey = key, SourceSample = key, Target = vi, Status = status };

    [Fact]
    public void KeyOf_IsStable_AndDistinguishesGame()
    {
        Assert.Equal(TranslationCache.KeyOf("fc26", "A"), TranslationCache.KeyOf("fc26", "A"));
        Assert.NotEqual(TranslationCache.KeyOf("fc26", "A"), TranslationCache.KeyOf("fc27", "A"));
        Assert.NotEqual(TranslationCache.KeyOf("fc26", "A"), TranslationCache.KeyOf("fc26", "a")); // phân biệt hoa/thường ở khoá cache
    }

    [Fact]
    public void UserWinsOverSeed_ExceptReviewedSeed()
    {
        var dir = Temp();
        var seed = Path.Combine(dir, "seed.json");
        var user = Path.Combine(dir, "user.json");
        var s = TranslationCache.Load(null, seed);
        s.Put(E("Inbox", "Hộp thư (seed)")); s.Put(E("Board", "Ban (seed reviewed)", "reviewed")); s.Save();
        var u = TranslationCache.Load(null, user);
        u.Put(E("Inbox", "Hộp thư (user)")); u.Put(E("Board", "Ban (user)")); u.Save();

        var merged = TranslationCache.Load(seed, user);
        Assert.Equal("Hộp thư (user)", merged.Get("fc26", "Inbox")!.Target);
        Assert.Equal("Ban (seed reviewed)", merged.Get("fc26", "Board")!.Target);
    }

    [Fact]
    public void Reviewed_IsNotOverwrittenByPut()
    {
        var c = TranslationCache.Load(null, Path.Combine(Temp(), "c.json"));
        Assert.True(c.Put(E("Inbox", "Hộp thư", "reviewed")));
        Assert.False(c.Put(E("Inbox", "Thư đến")));
        Assert.Equal("Hộp thư", c.Get("fc26", "Inbox")!.Target);
    }

    [Fact]
    public void Save_IsAtomic_AndReloadEqual()
    {
        var path = Path.Combine(Temp(), "cache", "fc26.json");
        var c = TranslationCache.Load(null, path);
        c.Put(E("Scout Report", "Báo cáo tuyển trạch"));
        c.Save();
        Assert.True(File.Exists(path));
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp")); // không để file tạm
        var back = TranslationCache.Load(null, path);
        Assert.Equal("Báo cáo tuyển trạch", back.Get("fc26", "Scout Report")!.Target);
    }

    [Theory]
    [InlineData("machine", 0, 1, 1, true)]
    [InlineData("rejected", 1, 1, 1, true)]    // mới 1 lần ⇒ còn 1 lần thử
    [InlineData("rejected", 2, 1, 1, false)]   // đủ 2 lần ⇒ dừng
    public void MayAttempt_Policy(string status, int attempts, int pv, int gv, bool expected)
    {
        var e = new TranslationEntry { Status = status, Attempts = attempts, PromptVersion = pv, GlossaryVersion = gv };
        Assert.Equal(expected, TranslationCache.MayAttempt(e, 2, 1, 1));
    }

    [Fact]
    public void MayAttempt_RetriesWhenPromptOrGlossaryVersionChanged_AndNullEntryAlways()
    {
        var e = new TranslationEntry { Status = "rejected", Attempts = 2, PromptVersion = 1, GlossaryVersion = 1 };
        Assert.False(TranslationCache.MayAttempt(e, 2, 1, 1));
        Assert.True(TranslationCache.MayAttempt(e, 2, 2, 1));
        Assert.True(TranslationCache.MayAttempt(e, 2, 1, 2));
        Assert.True(TranslationCache.MayAttempt(null, 2, 1, 1));
    }
}

public class UsageCounterTests
{
    private static string Temp() => Directory.CreateTempSubdirectory("vietsub-test-").FullName;

    [Fact]
    public void Reserve_UpToCap_ThenDenied_AndWarnsOnce()
    {
        var u = new UsageCounter(Temp(), 100, () => new DateOnly(2026, 11, 18));
        Assert.True(u.TryReserve(60));
        Assert.True(u.TryReserve(40));
        Assert.Equal(100, u.Used);
        Assert.False(u.TryReserve(1));
        Assert.True(u.CapReached);
        Assert.True(u.TakeCapWarning());
        Assert.False(u.TakeCapWarning());
    }

    [Fact]
    public void PersistsAcrossInstances_AndResetsNextDay()
    {
        var dir = Temp();
        var day = new DateOnly(2026, 11, 18);
        var a = new UsageCounter(dir, 100, () => day);
        Assert.True(a.TryReserve(70));
        var b = new UsageCounter(dir, 100, () => day);
        Assert.Equal(70, b.Used);
        Assert.False(b.TryReserve(40));
        day = day.AddDays(1);
        var c = new UsageCounter(dir, 100, () => day);
        Assert.Equal(0, c.Used);
        Assert.True(c.TryReserve(40));
        Assert.True(File.Exists(Path.Combine(dir, "2026-11-18.json")));
    }
}

public sealed class FakeBackend(Func<IReadOnlyList<BackendItem>, BackendResult> handler, int delayMs = 0) : ITranslationBackend
{
    public int Calls;
    public List<IReadOnlyList<BackendItem>> Batches { get; } = new();

    public async Task<BackendResult> TranslateAsync(IReadOnlyList<BackendItem> items, CancellationToken ct)
    {
        Interlocked.Increment(ref Calls);
        lock (Batches) Batches.Add(items);
        if (delayMs > 0) await Task.Delay(delayMs, ct);
        return handler(items);
    }

    public static BackendResult Ok(IReadOnlyList<BackendItem> items, Func<string, string>? vi = null)
        => new(BackendOutcome.Ok, items.Select(i => new BackendTranslated(i.Id, (vi ?? (s => "[VI] " + s))(i.En), null)).ToList(), null);
}

public class TranslationServiceTests
{
    private static string Temp() => Directory.CreateTempSubdirectory("vietsub-test-").FullName;
    private static NormalizedSource N(string key) => new(key, []);

    private sealed class Env
    {
        public DateTime Now = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        public TranslationCache Cache = TranslationCache.Load(null, Path.Combine(Temp(), "c.json"));
        public UsageCounter Usage = new(Temp(), 100000, () => new DateOnly(2026, 11, 18));
        public Glossary Glossary = new() { Version = 1, Terms = [new GlossaryTerm("Inbox", "Hộp thư", null, "ui")] };
        public TranslationService Svc(ITranslationBackend? b, TuningConfig? t = null) => new(Glossary, Cache, b, Usage, () => Now, t ?? new TuningConfig());
    }

    [Fact]
    public async Task Glossary_IsUsedWithoutBackend()
    {
        var e = new Env();
        var s = e.Svc(null);
        Assert.Equal("Hộp thư", s.Lookup("inbox")!.Text);
        Assert.Equal("glossary", s.Lookup("Inbox")!.Source);
        Assert.Equal(0, await s.ProcessAsync([N("Inbox")], default));
    }

    [Fact]
    public async Task NewSentences_AreTranslated_Cached_AndCounted()
    {
        var e = new Env();
        var fb = new FakeBackend(items => FakeBackend.Ok(items));
        var s = e.Svc(fb);
        var n = await s.ProcessAsync([N("Scout Report ready"), N("Your offer was accepted")], default);
        Assert.Equal(2, n);
        Assert.Equal(1, fb.Calls); // một lô
        Assert.Equal("[VI] Scout Report ready", s.Lookup("Scout Report ready")!.Text);
        Assert.Equal("machine", e.Cache.Get("fc26", "Scout Report ready")!.Status);
        Assert.Equal("Scout Report ready".Length + "Your offer was accepted".Length, e.Usage.Used);
        // lần sau không gọi nữa
        await s.ProcessAsync([N("Scout Report ready")], default);
        Assert.Equal(1, fb.Calls);
    }

    [Fact]
    public async Task IneligibleShortUppercase_IsNeverSentToBackend()
    {
        var e = new Env();
        var fb = new FakeBackend(items => FakeBackend.Ok(items));
        await e.Svc(fb).ProcessAsync([N("ACCEPT"), N("OK")], default);
        Assert.Equal(0, fb.Calls);
    }

    [Fact]
    public async Task BadSlot_IsRejected_Persisted_AttemptsCapAtTwo_AcrossSessions()
    {
        var e = new Env();
        var bad = new FakeBackend(items => new(BackendOutcome.Ok, items.Select(i => new BackendTranslated(i.Id, "sai slot", null)).ToList(), null));
        var key = "Budget: {N1}";
        var s1 = e.Svc(bad);
        Assert.Equal(0, await s1.ProcessAsync([N(key)], default));
        Assert.Equal(1, e.Cache.Get("fc26", key)!.Attempts);
        Assert.Equal("rejected", e.Cache.Get("fc26", key)!.Status);
        Assert.Null(s1.Lookup(key)); // không vẽ

        var s2 = e.Svc(bad); // "phiên" mới, cùng cache
        await s2.ProcessAsync([N(key)], default);
        Assert.Equal(2, e.Cache.Get("fc26", key)!.Attempts);
        Assert.Equal(2, bad.Calls);

        var s3 = e.Svc(bad);
        await s3.ProcessAsync([N(key)], default);
        Assert.Equal(2, bad.Calls); // đủ 2 lần ⇒ không gọi nữa
    }

    [Fact]
    public async Task RejectedSentence_IsRetriedAfterPromptVersionChange()
    {
        var e = new Env();
        var key = "Budget: {N1}";
        e.Cache.Put(new TranslationEntry { SourceKey = key, SourceSample = key, Status = "rejected", Attempts = 2, PromptVersion = TranslationService.PromptVersion - 1, GlossaryVersion = 1 });
        var fb = new FakeBackend(items => FakeBackend.Ok(items, s => "Ngân sách: {N1}"));
        Assert.Equal(1, await e.Svc(fb).ProcessAsync([N(key)], default));
        Assert.Equal(1, fb.Calls);
        Assert.Equal("machine", e.Cache.Get("fc26", key)!.Status);
    }

    [Fact]
    public async Task DroppedBatch_IsSplitInHalves_AndAttemptsOnlyCountOnSingleSentences()
    {
        var e = new Env();
        // lô nhiều câu bị bỏ; câu "poison" bị bỏ cả khi gửi riêng; câu tốt qua được khi lô nhỏ hơn
        var fb = new FakeBackend(items =>
            items.Any(i => i.En.Contains("poison")) ? new(BackendOutcome.Dropped, [], "refusal") : FakeBackend.Ok(items));
        var s = e.Svc(fb);
        var pending = new[] { "One good", "Two good", "Three poison", "Four good" }.Select(N).ToList();
        var n = await s.ProcessAsync(pending, default);
        Assert.Equal(3, n);
        Assert.Equal(0, e.Cache.Get("fc26", "One good") is { Attempts: > 0 } ? 1 : 0); // câu tốt không mất lượt oan
        Assert.Equal(1, e.Cache.Get("fc26", "Three poison")!.Attempts);                // chỉ câu đơn lẻ bị tính
        Assert.Equal("rejected", e.Cache.Get("fc26", "Three poison")!.Status);
    }

    [Fact]
    public async Task TransientError_DoesNotCountAttempt_ButBacksOff30s()
    {
        var e = new Env();
        var fail = true;
        var fb = new FakeBackend(items => fail ? new(BackendOutcome.Transient, [], "429") : FakeBackend.Ok(items));
        var s = e.Svc(fb);
        await s.ProcessAsync([N("Hello world")], default);
        Assert.Equal(1, fb.Calls);
        Assert.True(e.Cache.Get("fc26", "Hello world") is null or { Attempts: 0 });

        e.Now = e.Now.AddSeconds(10); fail = false;
        await s.ProcessAsync([N("Hello world")], default);
        Assert.Equal(1, fb.Calls); // còn trong 30 s lùi

        e.Now = e.Now.AddSeconds(25);
        Assert.Equal(1, await s.ProcessAsync([N("Hello world")], default));
        Assert.Equal(2, fb.Calls);
    }

    [Fact]
    public async Task InFlight_SentenceIsNotSentTwice_WhileWaiting()
    {
        var e = new Env();
        var fb = new FakeBackend(items => FakeBackend.Ok(items), delayMs: 300);
        var s = e.Svc(fb);
        var first = s.ProcessAsync([N("Slow sentence here")], default);
        await Task.Delay(50);
        Assert.Contains("Slow sentence here", s.InFlight);
        await s.ProcessAsync([N("Slow sentence here")], default); // màn quay lại cùng chữ ký
        await first;
        Assert.Equal(1, fb.Calls);
        Assert.Empty(s.InFlight);
    }

    [Fact]
    public async Task DailyCap_StopsBackendCalls_ButCacheStillServed()
    {
        var e = new Env();
        e.Usage = new UsageCounter(Temp(), 10, () => new DateOnly(2026, 11, 18));
        var fb = new FakeBackend(items => FakeBackend.Ok(items));
        var s = e.Svc(fb);
        await s.ProcessAsync([N("Short one")], default);              // 9 ký tự — qua
        Assert.Equal(1, fb.Calls);
        await s.ProcessAsync([N("Another sentence that is long")], default); // vượt trần
        Assert.Equal(1, fb.Calls);
        Assert.NotNull(s.Lookup("Short one"));
        Assert.True(e.Usage.CapReached);
    }

    [Fact]
    public async Task NoBackend_MeansCacheOnly_NoException()
    {
        var e = new Env();
        var s = e.Svc(null);
        Assert.Equal(0, await s.ProcessAsync([N("Never seen before")], default));
        Assert.Null(s.Lookup("Never seen before"));
    }

    [Fact]
    public async Task BatchIsCappedAtBatchMax()
    {
        var e = new Env();
        var fb = new FakeBackend(items => FakeBackend.Ok(items));
        var pending = Enumerable.Range(0, 7).Select(i => N($"Sentence number {i} ok")).ToList();
        await e.Svc(fb, new TuningConfig { BatchMax = 3 }).ProcessAsync(pending, default);
        Assert.All(fb.Batches, b => Assert.True(b.Count <= 3));
        Assert.True(fb.Calls >= 3);
    }
}
