using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Engine.Config;

namespace Engine.Translation;

public sealed record NormalizedSource(string Key, IReadOnlyList<string> Slots);

public static class SourceNormalizer
{
    private static readonly Regex Variables = new(
        @"(?<time>\b\d{1,2}:\d{2}\b)"
        + @"|(?<date>\b\d{1,2}\s(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\.?(?:\s\d{4})?\b|\b\d{1,2}/\d{1,2}/\d{2,4}\b)"
        + @"|(?<money>[€£$]\s?\d[\d,]*(?:\.\d+)?\s?[KMBkmb]?(?:/(?:wk|yr|w|y))?(?![A-Za-z]))"
        + @"|(?<pct>\d+(?:\.\d+)?\s?%)"
        + @"|(?<ovr>\b(?:OVR|POT)\s?\d{2,3}\b)"
        + @"|(?<num>\b\d[\d,]*(?:\.\d+)?\b)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SlotToken = new(@"\{N(\d+)\}", RegexOptions.Compiled);

    /// <summary>Chuẩn hoá khoảng trắng + dấu nháy, che biến thành slot {N1}… theo thứ tự xuất hiện (số, tiền, %, ngày, giờ, OVR).</summary>
    public static NormalizedSource Normalize(string raw)
    {
        var s = raw.Replace('’', '\'').Replace('‘', '\'').Replace('“', '"').Replace('”', '"');
        s = Regex.Replace(s, @"\s+", " ").Trim();
        var slots = new List<string>();
        var masked = Variables.Replace(s, m =>
        {
            slots.Add(m.Value);
            return "{N" + slots.Count + "}";
        });
        return new NormalizedSource(masked, slots);
    }

    /// <summary>Thay {Nk} trong bản dịch bằng giá trị gốc.</summary>
    public static string Restore(string target, IReadOnlyList<string> slots)
        => SlotToken.Replace(target, m =>
        {
            int k = int.Parse(m.Groups[1].Value);
            return k >= 1 && k <= slots.Count ? slots[k - 1] : m.Value;
        });
}

public static class SlotValidator
{
    private static readonly Regex Token = new(@"\{N\d+\}", RegexOptions.Compiled);

    public static IReadOnlySet<string> SlotsOf(string text)
        => Token.Matches(text).Select(m => m.Value).ToHashSet();

    /// <summary>Tập slot của bản dịch phải ĐÚNG bằng tập slot của nguồn.</summary>
    public static bool Validate(string sourceKey, string target)
        => SlotsOf(sourceKey).SetEquals(SlotsOf(target));
}

public static class LlmEligibility
{
    /// <summary>N4: dòng chỉ chữ hoa ≤ 3 từ hoặc ≤ 4 ký tự ⇒ không gọi LLM.</summary>
    public static bool IsEligible(string normalizedKey)
    {
        var k = normalizedKey.Trim();
        if (k.Length <= 4) return false;
        bool anyLower = k.Any(char.IsLower);
        int words = k.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (!anyLower && words <= 3) return false;
        return true;
    }
}

public sealed record GlossaryTerm(
    [property: JsonPropertyName("term_en")] string TermEn,
    [property: JsonPropertyName("term_vi")] string TermVi,
    [property: JsonPropertyName("short_vi")] string? ShortVi,
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("doNotTranslate")] bool DoNotTranslate = false);

public sealed class Glossary
{
    private Dictionary<string, GlossaryTerm>? _index;

    public int Version { get; init; }
    public IReadOnlyList<GlossaryTerm> Terms { get; init; } = [];

    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static Glossary Load(string path)
        => JsonSerializer.Deserialize<Glossary>(File.ReadAllText(path), Opts)
           ?? throw new InvalidDataException($"Glossary rỗng: {path}");

    public GlossaryTerm? Lookup(string normalizedKey)
    {
        _index ??= Terms.GroupBy(t => t.TermEn.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First());
        return _index.TryGetValue(normalizedKey.Trim().ToLowerInvariant(), out var t) ? t : null;
    }
}

public sealed class TranslationEntry
{
    public string GameId { get; set; } = "fc26";
    public string SourceKey { get; set; } = "";
    public string SourceSample { get; set; } = "";
    public string Target { get; set; } = "";
    public string? ShortVi { get; set; }
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public int GlossaryVersion { get; set; }
    public int PromptVersion { get; set; }
    /// <summary>machine | reviewed | rejected</summary>
    public string Status { get; set; } = "machine";
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int HitCount { get; set; }
    public DateTime? LastHitAt { get; set; }
}

public sealed class TranslationCache
{
    private sealed class FileModel { public List<TranslationEntry> Entries { get; set; } = new(); }

    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly Dictionary<string, TranslationEntry> _map = new();
    private readonly HashSet<string> _user = new();
    private readonly object _gate = new();
    private readonly object _saveGate = new();
    private string _userPath = "";

    public static string KeyOf(string gameId, string sourceKey) => gameId + "\u001f" + sourceKey;

    /// <summary>Nạp seed (repo) trước, rồi cache người dùng: người dùng thắng, trừ khi seed là <c>reviewed</c>.</summary>
    public static TranslationCache Load(string? seedPath, string userPath)
    {
        var c = new TranslationCache { _userPath = userPath };
        if (seedPath is not null && File.Exists(seedPath))
            foreach (var e in Read(seedPath)) c._map[KeyOf(e.GameId, e.SourceKey)] = e;
        if (File.Exists(userPath))
            foreach (var e in Read(userPath))
            {
                var k = KeyOf(e.GameId, e.SourceKey);
                if (c._map.TryGetValue(k, out var seed) && seed.Status == "reviewed") continue;
                c._map[k] = e;
                c._user.Add(k);
            }
        return c;
    }

    private static List<TranslationEntry> Read(string path)
        => (JsonSerializer.Deserialize<FileModel>(File.ReadAllText(path), Opts) ?? new FileModel()).Entries;

    public TranslationEntry? Get(string gameId, string sourceKey)
    {
        lock (_gate) return _map.TryGetValue(KeyOf(gameId, sourceKey), out var e) ? e : null;
    }

    public IReadOnlyCollection<TranslationEntry> All
    {
        get { lock (_gate) return _map.Values.ToList(); }
    }

    /// <summary>
    /// Ghi/ thay entry; entry <c>reviewed</c> KHÔNG bị ghi đè tự động (trả false).
    /// <paramref name="persist"/> = false ⇒ chỉ giữ trong bộ nhớ, <see cref="Save"/> không ghi (bản dịch giả — NB5).
    /// </summary>
    public bool Put(TranslationEntry e, bool persist = true)
    {
        lock (_gate)
        {
            var k = KeyOf(e.GameId, e.SourceKey);
            if (_map.TryGetValue(k, out var old) && old.Status == "reviewed" && e.Status != "reviewed") return false;
            _map[k] = e;
            if (persist) _user.Add(k); else _user.Remove(k);
            return true;
        }
    }

    /// <summary>Ghi file nguyên tử (temp + rename). Chỉ ghi phần người dùng (không ghi lại seed chưa đổi).</summary>
    public void Save()
    {
        // NB6: một khoá cho cả đọc-bản-chụp lẫn ghi file ⇒ hai Save (dịch nền chạy chồng) không giẫm lên cùng file .tmp.
        lock (_saveGate)
        {
            string json;
            lock (_gate)
                json = JsonSerializer.Serialize(new FileModel { Entries = _user.Select(k => _map[k]).OrderBy(e => e.SourceKey, StringComparer.Ordinal).ToList() }, Opts);
            var dir = Path.GetDirectoryName(Path.GetFullPath(_userPath))!;
            Directory.CreateDirectory(dir);
            var tmp = _userPath + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _userPath, overwrite: true);
        }
    }

    /// <summary>B1: entry rejected có attempts ≥ max ⇒ chỉ thử lại khi promptVersion/glossaryVersion đổi.</summary>
    public static bool MayAttempt(TranslationEntry? e, int maxAttempts, int promptVersion, int glossaryVersion)
    {
        if (e is null || e.Status != "rejected") return true;
        if (e.PromptVersion != promptVersion || e.GlossaryVersion != glossaryVersion) return true;
        return e.Attempts < maxAttempts;
    }
}

public sealed class UsageCounter(string dir, int cap, Func<DateOnly> today)
{
    private readonly object _gate = new();
    private DateOnly _day;
    private int _used;
    private bool _loaded, _capHit, _warned;

    private string PathFor(DateOnly d) => Path.Combine(dir, d.ToString("yyyy-MM-dd") + ".json");

    private void Sync()
    {
        var d = today();
        if (_loaded && d == _day) return;
        _day = d; _loaded = true; _capHit = false; _warned = false; _used = 0;
        var p = PathFor(d);
        if (File.Exists(p))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(p));
                _used = doc.RootElement.GetProperty("chars").GetInt32();
                if (_used < 0) throw new InvalidDataException("chars < 0");
            }
            catch (Exception)
            {
                // NB4: file hỏng/ sai kiểu ⇒ fail-CLOSED: coi như đã chạm trần hôm nay (không reset trần, không làm chết `run`).
                _used = cap; _capHit = true;
                return;
            }
        }
        if (_used >= cap) _capHit = true;
    }

    public int Used { get { lock (_gate) { Sync(); return _used; } } }


    public bool CapReached { get { lock (_gate) { Sync(); return _capHit; } } }

    /// <summary>true nếu còn đủ trong trần ngày và đã cộng; false ⇒ không gọi LLM (đặt CapReached, cảnh báo một lần).</summary>
    public bool TryReserve(int chars)
    {
        lock (_gate)
        {
            Sync();
            if (_used + chars > cap) { _capHit = true; return false; }
            _used += chars;
            Persist();
            return true;
        }
    }

    /// <summary>Hoàn lại ký tự đã giữ chỗ (lỗi cấu hình / 429 / 5xx — yêu cầu không bị tính tiền).</summary>
    public void Refund(int chars)
    {
        lock (_gate)
        {
            Sync();
            _used = Math.Max(0, _used - chars);
            if (_used < cap) _capHit = false;
            Persist();
        }
    }

    // NB4: ghi nguyên tử (temp + rename); lỗi I/O không ném ra ngoài (giá trị trong bộ nhớ vẫn đúng).
    private void Persist()
    {
        try
        {
            Directory.CreateDirectory(dir);
            var p = PathFor(_day);
            var tmp = p + ".tmp";
            File.WriteAllText(tmp, "{\"chars\":" + _used + "}");
            File.Move(tmp, p, overwrite: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>Trả true đúng MỘT lần sau khi chạm trần (để in cảnh báo một lần).</summary>
    public bool TakeCapWarning()
    {
        lock (_gate)
        {
            Sync();
            if (!_capHit || _warned) return false;
            _warned = true;
            return true;
        }
    }
}

// ---- Backend (LLM) ----
public sealed record BackendItem(string Id, string En);

public sealed record BackendTranslated(string Id, string Vi, string? ShortVi);

public enum BackendOutcome { Ok, Dropped, Transient, Fatal }

public sealed record BackendResult(BackendOutcome Outcome, IReadOnlyList<BackendTranslated> Items, string? Error, bool Refund = false);

public interface ITranslationBackend
{
    Task<BackendResult> TranslateAsync(IReadOnlyList<BackendItem> items, CancellationToken ct);

    /// <summary>Backend giả (smoke/test): không ghi cache người dùng, không trừ trần ngày (NB5).</summary>
    bool IsFake => false;

    string Provider => "anthropic";
}

public sealed record TranslationResult(string Text, string? ShortVi, string Source);

/// <summary>Glossary → cache → LLM, với chống gọi trả phí lặp (B1): rejected/attempts qua phiên, inFlight, lùi mạng, tách đôi lô.</summary>
public sealed class TranslationService(
    Glossary glossary, TranslationCache cache, ITranslationBackend? backend, UsageCounter usage,
    Func<DateTime> utcNow, TuningConfig t, string gameId = "fc26", Action<string>? log = null)
{
    public const int PromptVersion = 1;

    private readonly object _gate = new();
    private readonly HashSet<string> _inFlight = new();
    private readonly HashSet<string> _triedThisSession = new(); // NB1: một câu bị loại chỉ được thử lại ở PHIÊN SAU
    private DateTime _backoffUntil = DateTime.MinValue;
    private int _calls;
    private volatile bool _disabled;
    private string? _disabledReason;

    public int BackendCalls => Volatile.Read(ref _calls);

    /// <summary>B2: lỗi cấu hình/ xác thực (400/401/403/404…) ⇒ backend LLM bị tắt cả phiên (một dòng log, không gọi lại).</summary>
    public bool BackendDisabled => _disabled;
    public string? DisabledReason { get { lock (_gate) return _disabledReason; } }

    public IReadOnlyCollection<string> InFlight { get { lock (_gate) return _inFlight.ToList(); } }

    /// <summary>Có bản dịch dùng được ngay (glossary hoặc cache không rejected)? Tên riêng doNotTranslate trả nguyên văn.</summary>
    public TranslationResult? Lookup(string sourceKey)
    {
        var g = glossary.Lookup(sourceKey);
        if (g is not null) return new TranslationResult(g.TermVi, g.ShortVi, "glossary");
        var e = cache.Get(gameId, sourceKey);
        if (e is { Status: not "rejected" } && !string.IsNullOrWhiteSpace(e.Target))
            return new TranslationResult(e.Target, e.ShortVi, e.Status == "reviewed" ? "reviewed" : "cache");
        return null;
    }

    /// <returns>số câu mới có bản dịch.</returns>
    public async Task<int> ProcessAsync(IReadOnlyList<NormalizedSource> pending, CancellationToken ct)
    {
        if (backend is null || _disabled) return 0;
        var todo = new List<string>();
        lock (_gate)
        {
            if (utcNow() < _backoffUntil) return 0;
            foreach (var key in pending.Select(p => p.Key).Distinct())
            {
                if (Lookup(key) is not null) continue;
                if (!LlmEligibility.IsEligible(key)) continue;
                if (!TranslationCache.MayAttempt(cache.Get(gameId, key), t.MaxAttempts, PromptVersion, glossary.Version)) continue;
                if (_inFlight.Contains(key)) continue;
                if (_triedThisSession.Contains(key)) continue;
                todo.Add(key);
                _inFlight.Add(key); // chiếm chỗ ngay: màn quay về cùng chữ ký không gửi lại
            }
        }

        int added = 0;
        var remaining = new Queue<string>(todo);
        try
        {
            while (remaining.Count > 0 && !ct.IsCancellationRequested)
            {
                var batch = new List<string>();
                while (batch.Count < Math.Max(1, t.BatchMax) && remaining.Count > 0) batch.Add(remaining.Dequeue());
                var (n, stop) = await TranslateBatchAsync(batch, ct);
                added += n;
                cache.Save();
                if (stop || _disabled) break;
            }
        }
        finally
        {
            lock (_gate) foreach (var k in todo) _inFlight.Remove(k);
        }
        return added;
    }

    /// <returns>(số câu mới, dừng các lô còn lại?)</returns>
    private async Task<(int added, bool stop)> TranslateBatchAsync(List<string> keys, CancellationToken ct)
    {
        if (_disabled) return (0, true);
        bool fake = backend!.IsFake;                      // NB5: bản dịch giả không trừ trần, không ghi cache người dùng
        int chars = keys.Sum(k => k.Length);
        if (!fake && !usage.TryReserve(chars)) return (0, true); // chạm trần ngày: dừng gọi LLM

        var items = keys.Select((k, i) => new BackendItem("s" + (i + 1), k)).ToList();
        Interlocked.Increment(ref _calls);
        var r = await backend!.TranslateAsync(items, ct);

        switch (r.Outcome)
        {
            case BackendOutcome.Transient:
                lock (_gate) _backoffUntil = utcNow().AddMilliseconds(t.BackoffMs); // KHÔNG tính attempts
                if (r.Refund && !fake) usage.Refund(chars);                          // NB3: 429/5xx không bị tính tiền
                return (0, true);

            case BackendOutcome.Fatal:
                // B2: không tách đôi, không attempts, hoàn ký tự đã giữ chỗ, tắt backend cả phiên.
                if (!fake) usage.Refund(chars);
                lock (_gate) { _disabled = true; _disabledReason = r.Error ?? "lỗi không thể thử lại"; }
                log?.Invoke($"[llm] tắt backend LLM cho phiên này: {_disabledReason}. Cache/glossary vẫn dùng; sửa khoá/cấu hình rồi chạy lại.");
                return (0, true);

            case BackendOutcome.Dropped:
                if (keys.Count == 1) { Reject(keys[0], r.Error ?? "dropped"); return (0, false); }
                int half = keys.Count / 2;
                var (a1, s1) = await TranslateBatchAsync(keys.Take(half).ToList(), ct);
                if (s1) return (a1, true);
                var (a2, s2) = await TranslateBatchAsync(keys.Skip(half).ToList(), ct);
                return (a1 + a2, s2);

            default:
                int ok = 0;
                var byId = r.Items.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());
                foreach (var item in items)
                {
                    if (!byId.TryGetValue(item.Id, out var tr)) { Reject(item.En, "missing"); continue; }
                    if (string.IsNullOrWhiteSpace(tr.Vi) || !SlotValidator.Validate(item.En, tr.Vi)) { Reject(item.En, "slot"); continue; }
                    var shortVi = tr.ShortVi is { Length: > 0 } sv && SlotValidator.Validate(item.En, sv) ? sv : null;
                    cache.Put(new TranslationEntry
                    {
                        GameId = gameId, SourceKey = item.En, SourceSample = item.En, Target = tr.Vi, ShortVi = shortVi,
                        Provider = backend.Provider, Model = fake ? "fake" : ClaudePrompt.Model, GlossaryVersion = glossary.Version,
                        PromptVersion = PromptVersion, Status = "machine", CreatedAt = utcNow(),
                    }, persist: !fake);
                    ok++;
                }
                return (ok, false);
        }
    }

    private void Reject(string key, string error)
    {
        lock (_gate) _triedThisSession.Add(key);
        if (backend?.IsFake == true) return;
        var old = cache.Get(gameId, key);
        bool sameVersion = old is not null && old.PromptVersion == PromptVersion && old.GlossaryVersion == glossary.Version;
        cache.Put(new TranslationEntry
        {
            GameId = gameId, SourceKey = key, SourceSample = key, Target = "", Provider = "anthropic", Model = ClaudePrompt.Model,
            GlossaryVersion = glossary.Version, PromptVersion = PromptVersion, Status = "rejected",
            Attempts = (sameVersion ? old!.Attempts : 0) + 1, LastError = error, CreatedAt = utcNow(),
        });
    }
}
