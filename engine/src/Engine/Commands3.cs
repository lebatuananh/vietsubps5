using System.Text;
using System.Text.Json;
using Engine.Capture;
using Engine.Config;
using Engine.Geometry;
using Engine.Layout;
using Engine.Ocr;
using Engine.Pipeline;
using Engine.Protocol;
using Engine.Translation;

namespace Engine;

/// <summary>Ghép các thành phần Phase 3 từ cấu hình + tham số dòng lệnh. Mọi đường dẫn dữ liệu lấy từ VIETSUB_HOME (N11).</summary>
public sealed class Runtime
{
    public string Home { get; init; } = "";
    public EngineConfig Cfg { get; init; } = new();
    public Glossary Glossary { get; init; } = new();
    public TranslationCache Cache { get; init; } = null!;
    public UsageCounter Usage { get; init; } = null!;
    public TranslationService Svc { get; init; } = null!;
    public string TranslatorName { get; init; } = "cache";
    public ClaudeBackend? Claude { get; init; }
    public FakeTranslator? Fake { get; init; }
    public bool HasKey { get; init; }

    public static Runtime Create(Args a, Action<string> log)
    {
        string home = VietSubHome.Resolve();
        var cfg = EngineConfig.Load(home);
        var glossary = Glossary.Load(DataPaths.Fc26("glossary.json"));
        var cache = TranslationCache.Load(DataPaths.Fc26("seed-cache.json"), Path.Combine(home, "cache", "fc26.json"));
        var usage = new UsageCounter(Path.Combine(home, "usage"), cfg.Tuning.DailyCapChars, () => DateOnly.FromDateTime(DateTime.Now));

        string name = (a.Get("translator") ?? "cache").ToLowerInvariant();
        ITranslationBackend? backend = null;
        ClaudeBackend? claude = null; FakeTranslator? fake = null; bool hasKey = false;
        switch (name)
        {
            case "fake": backend = fake = new FakeTranslator(); break;
            case "cache": break;
            case "claude":
                var key = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"); // không bao giờ log/ghi giá trị
                if (string.IsNullOrWhiteSpace(key))
                    log("[llm] cảnh báo: không có ANTHROPIC_API_KEY — chỉ dùng cache/glossary (không gọi LLM).");
                else { hasKey = true; backend = claude = new ClaudeBackend(new AnthropicSdkTransport(key), glossary, log); }
                break;
            default: throw new ArgumentException($"--translator phải là claude|cache|fake, nhận '{name}'.");
        }
        var svc = new TranslationService(glossary, cache, backend, usage, () => DateTime.UtcNow, cfg.Tuning, log: log);
        return new Runtime { Home = home, Cfg = cfg, Glossary = glossary, Cache = cache, Usage = usage, Svc = svc, TranslatorName = name, Claude = claude, Fake = fake, HasKey = hasKey };
    }

    /// <summary>HTTP thật: chỉ khi có transport SDK (translator claude + có khoá); fake/cache = 0.</summary>
    public int HttpCalls => Claude is null ? 0 : Svc.BackendCalls;
}

public static class Commands3
{
    private static IFrameSink MakeSink(Runtime rt, bool send, Action<string> log, out TvClient? client)
    {
        client = null;
        if (!send) return new NullSink();
        if (string.IsNullOrWhiteSpace(rt.Cfg.Tv.Host) || string.IsNullOrWhiteSpace(rt.Cfg.Tv.Token))
            throw new EngineException(1, "Chưa ghép cặp với TV: chạy `engine pair --tv <ip> --code <mã trên màn TV>` trước (hoặc dùng --no-send).");
        client = new TvClient(rt.Cfg.Tv.Host!, rt.Cfg.Tv.Port, rt.Cfg.Tv.Token!, log);
        client.Start();
        return client;
    }

    // ------------------------------------------------------------------ pair
    public static async Task<int> PairAsync(Args a)
    {
        string host = a.Require("tv");
        string code = a.Require("code");
        int port = a.GetInt("port", ProtocolJson.Port);
        if (!PairCode.IsValid(code))
            throw new EngineException(1, $"Mã ghép cặp không hợp lệ: cần 8 ký tự thuộc {PairCode.Alphabet} (dạng XXXX-XXXX).");
        string token = PairCode.Normalize(code);

        string home = VietSubHome.Resolve();
        var cfg = EngineConfig.Load(home);
        cfg.Tv.Host = host; cfg.Tv.Port = port; cfg.Tv.Token = token;
        cfg.Save(home);
        Console.WriteLine($"[pair] đã ghi {EngineConfig.PathIn(home)} (host={host} port={port}; mã không in ra).");

        var reply = await TvClient.TestHelloAsync(host, port, token, 3000);
        if (reply is null) throw new EngineException(2, $"Không kết nối được tới TV {host}:{port} (kiểm IP, app TV đang chạy, cùng mạng).");
        if (!reply.Ok) throw new EngineException(3, $"TV từ chối: {reply.Reason} — {(reply.Reason == "token" ? "sai mã ghép cặp" : "khác phiên bản giao thức")}.");
        Console.WriteLine("[pair] OK — TV chấp nhận mã.");
        return 0;
    }

    // ------------------------------------------------------------------ pattern
    public static async Task<int> PatternAsync(Args a)
    {
        bool send = a.Get("send") is "true";
        int seconds = a.GetInt("seconds", 60);
        Action<string> log = m => Console.WriteLine(m);
        var rt = Runtime.Create(a, log);
        var frame = BuildPattern(rt.Cfg, Guid.NewGuid().ToString("N"), 1);
        var line = ProtocolJson.SerializeFrame(frame);
        if (!send) { Console.WriteLine(line); return 0; }

        var sink = MakeSink(rt, true, log, out var client);
        await using var _ = client!;
        long seq = 1;
        var sid = frame.SessionId;
        for (int i = 0; i < seconds; i++)
        {
            sink.Send(ProtocolJson.SerializeFrame(BuildPattern(rt.Cfg, sid, seq++)));
            await Task.Delay(1000);
        }
        sink.Send(ProtocolJson.SerializeFrame(new OverlayFrame(1, sid, seq++, 3000, null, [])));
        await Task.Delay(300);
        return 0;
    }

    public static OverlayFrame BuildPattern(EngineConfig cfg, string sessionId, long seq)
    {
        const string yellow = "#FFFFD400", black = "#FF000000";
        OverlayItem Mk(string id, string text, double x, double y, double w, double h, string align = "start") =>
            new(id, new Box(x, y, w, h), text, null, 26, "pattern", 1, align, true, black, new CoverSpec("solid", yellow), null);
        var items = new List<OverlayItem>
        {
            Mk("p-tl", "TL", 0.0, 0.0, 0.05, 0.05),
            Mk("p-tr", "TR", 0.95, 0.0, 0.05, 0.05, "end"),
            Mk("p-bl", "BL", 0.0, 0.95, 0.05, 0.05),
            Mk("p-br", "BR", 0.95, 0.95, 0.05, 0.05, "end"),
        };
        for (int i = 1; i <= 9; i++)
        {
            items.Add(Mk($"p-t{i}", $"{i * 10}%", i / 10.0 - 0.02, 0.0, 0.04, 0.04, "center"));
            items.Add(Mk($"p-l{i}", $"{i * 10}%", 0.0, i / 10.0 - 0.02, 0.05, 0.04));
        }
        return new OverlayFrame(1, sessionId, seq, 3000, new DisplaySpec(cfg.Display.MinFontPx, cfg.Display.HoldMs), items);
    }

    // ------------------------------------------------------------------ replay
    public static async Task<int> ReplayAsync(Args a)
    {
        string input = a.Require("input");
        string outDir = a.Get("out") ?? Path.Combine(input, "_replay");
        bool send = a.Get("send") is "true";
        int holdMs = a.GetInt("hold-ms", 4000);
        bool verbose = a.Get("verbose") is "true";
        if (!Directory.Exists(input)) throw new EngineException(1, $"Thư mục --input không tồn tại: {input}");
        Action<string> log = m => Console.WriteLine(m);
        var rt = Runtime.Create(a, log);
        var sink = MakeSink(rt, send, log, out var client);
        await using var _ = client;
        var ocr = new WindowsOcrProvider(OcrRunner.CreateEngine());
        var pipe = new FramePipeline(rt.Cfg, ocr, rt.Svc, sink, Guid.NewGuid().ToString("N"), log, verbose);
        var src = new PngDirSource(input, rt.Cfg.Tuning.FrameIntervalMs, holdMs);
        if (src.Files.Count == 0) throw new EngineException(1, $"Không có .png trong {input}");

        Directory.CreateDirectory(outDir);
        var misses = new List<object>();
        await foreach (var (file, frames) in src.ReadAsync())
        {
            long before = pipe.SentCount; int changesBefore = pipe.ItemSetChanges;
            foreach (var f in frames)
            {
                await pipe.OnFrameAsync(f, default);
                if (send) await Task.Delay(rt.Cfg.Tuning.FrameIntervalMs);
            }
            await pipe.WaitTranslationsAsync();
            var last = frames[^1];
            await pipe.OnFrameAsync(last with { TimestampMs = last.TimestampMs + 1 }, default); // nhận bản dịch vừa về (D5)
            string stem = Path.GetFileNameWithoutExtension(file);
            var sent = pipe.LastSent;
            File.WriteAllText(Path.Combine(outDir, stem + ".frame.json"),
                JsonSerializer.Serialize(sent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            foreach (var m in pipe.LastMissing)
                misses.Add(new { image = Path.GetFileName(file), sourceKey = m.Key, reason = MissReason(rt, m.Key) });
            log($"[replay] {Path.GetFileName(file)}: {sent?.Items.Count ?? 0} item · gói {pipe.SentCount - before} · bộ item đổi {pipe.ItemSetChanges - changesBefore} lần · chưa dịch {pipe.LastMissing.Count}");
        }
        File.WriteAllText(Path.Combine(outDir, "misses.json"),
            JsonSerializer.Serialize(misses, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        rt.Cache.Save();
        log($"[replay] translator={rt.TranslatorName} · gọi backend: {rt.Svc.BackendCalls} · HTTP thật: {rt.HttpCalls} · ký tự LLM hôm nay: {rt.Usage.Used}/{rt.Cfg.Tuning.DailyCapChars} · misses: {misses.Count} → {outDir}");
        return 0;
    }

    private static string MissReason(Runtime rt, string key)
    {
        if (!LlmEligibility.IsEligible(key)) return "not-eligible (chữ hoa ngắn/ ≤ 4 ký tự — chỉ glossary/cache)";
        var e = rt.Cache.Get("fc26", key);
        if (e is { Status: "rejected" }) return $"rejected (attempts={e.Attempts}, {e.LastError})";
        if (rt.TranslatorName == "cache" || (rt.TranslatorName == "claude" && !rt.HasKey)) return "no-translator (chỉ cache/glossary)";
        return "pending/chưa có bản dịch";
    }

    // ------------------------------------------------------------------ run
    public static async Task<int> RunAsync(Args a)
    {
        bool send = a.Get("no-send") is not "true";
        bool verbose = a.Get("verbose") is "true";
        Action<string> log = m => Console.WriteLine(m);
        var rt = Runtime.Create(a, log);
        var sink = MakeSink(rt, send, log, out var client);
        await using var _ = client;
        var ocr = new WindowsOcrProvider(OcrRunner.CreateEngine());
        var pipe = new FramePipeline(rt.Cfg, ocr, rt.Svc, sink, Guid.NewGuid().ToString("N"), log, verbose);
        string title = a.Get("window-title") ?? "PS Remote Play";
        var hwndArg = a.Get("hwnd");

        using var cts = new CancellationTokenSource();
        int maxSeconds = a.GetInt("max-seconds", 0); // tham số hỗ trợ smoke: tự dừng sau N giây (như Ctrl+C)
        if (maxSeconds > 0) cts.CancelAfter(maxSeconds * 1000);
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        var clock = System.Diagnostics.Stopwatch.StartNew();
        long nextUsageLog = 10 * 60 * 1000;
        log($"[run] bắt đầu — cửa sổ \"{title}\" · translator={rt.TranslatorName} · gửi TV: {(send ? $"{rt.Cfg.Tv.Host}:{rt.Cfg.Tv.Port}" : "KHÔNG (--no-send)")} · Ctrl+C để dừng");

        while (!cts.IsCancellationRequested)
        {
            try
            {
                WindowInfo win;
                if (hwndArg is not null)
                    win = WindowFinder.FromHwnd(new IntPtr(long.Parse(hwndArg.StartsWith("0x") ? hwndArg[2..] : hwndArg, hwndArg.StartsWith("0x") ? System.Globalization.NumberStyles.HexNumber : System.Globalization.NumberStyles.Integer)))
                          ?? throw new EngineException(2, "HWND không hợp lệ.");
                else
                {
                    var found = WindowFinder.FindByTitle(title);
                    if (found.Count != 1) throw new EngineException(2, found.Count == 0 ? $"Chưa thấy cửa sổ \"{title}\"." : $"{found.Count} cửa sổ khớp — dùng --hwnd.");
                    win = found[0];
                }
                log($"[run] chụp {win}");
                using var session = new CaptureSession(win, true, recreateOnResize: true);
                int lastRecreations = 0;
                while (!cts.IsCancellationRequested)
                {
                    var cap = session.Grab(5000);
                    if (session.Recreations != lastRecreations) { lastRecreations = session.Recreations; log($"[run] cửa sổ đổi cỡ — đã tạo lại frame pool ({cap.Width}x{cap.Height})"); }
                    await pipe.OnFrameAsync(new SourceFrame(cap.Pixels, cap.Width, cap.Height, clock.ElapsedMilliseconds, null), cts.Token);
                    if (clock.ElapsedMilliseconds >= nextUsageLog) { log($"[usage] ký tự LLM hôm nay: {rt.Usage.Used}/{rt.Cfg.Tuning.DailyCapChars}"); nextUsageLog += 10 * 60 * 1000; }
                    if (rt.Usage.TakeCapWarning()) log($"[usage] CHẠM TRẦN {rt.Cfg.Tuning.DailyCapChars} ký tự/ngày — ngừng gọi LLM tới hết ngày (vẫn dùng cache).");
                    await Task.Delay(rt.Cfg.Tuning.FrameIntervalMs, cts.Token);
                }
            }
            catch (EngineException ex) when (ex.ExitCode is 2 or 3 or 4 or 6)
            {
                // D4: cửa sổ thu nhỏ/ không thấy ⇒ ngừng gửi (TV tự xoá theo ttl), thử lại mỗi 2 s, KHÔNG thoát.
                log($"[run] {ex.Message} — thử lại sau 2 s");
                pipe.SendEmpty(clock.ElapsedMilliseconds);
                try { await Task.Delay(2000, cts.Token); } catch (OperationCanceledException) { break; }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) when (ex is not EngineException)
            {
                // NB13: ngoại lệ COM/IO lạ từ chụp/OCR không được làm `run` thoát — log loại + thông điệp, ngừng gửi, thử lại sau 2 s.
                log($"[run] lỗi bất ngờ {ex.GetType().Name}: {ex.Message} — thử lại sau 2 s");
                pipe.SendEmpty(clock.ElapsedMilliseconds);
                try { await Task.Delay(2000, cts.Token); } catch (OperationCanceledException) { break; }
            }
        }

        pipe.SendEmpty(clock.ElapsedMilliseconds); // Ctrl+C ⇒ items:[] rồi thoát
        await Task.Delay(300);
        rt.Cache.Save();
        log($"[usage] ký tự LLM hôm nay: {rt.Usage.Used}/{rt.Cfg.Tuning.DailyCapChars} · gọi backend: {rt.Svc.BackendCalls} · HTTP thật: {rt.HttpCalls}");
        return 0;
    }

    // ------------------------------------------------------------------ warm
    public static async Task<int> WarmAsync(Args a)
    {
        string input = a.Require("input");
        bool yes = a.Get("yes") is "true";
        Action<string> log = m => Console.WriteLine(m);
        var rt = Runtime.Create(a, log);
        var ocr = OcrRunner.CreateEngine();
        var missing = new Dictionary<string, NormalizedSource>();
        foreach (var f in Directory.GetFiles(input, "*.png").OrderBy(x => x))
        {
            var img = await ImageIo.ReadBgraAsync(f);
            var det = ContentRectDetector.Detect(img.Pixels, img.Width, img.Height, img.Stride);
            var lines = await OcrRunner.RecognizeLinesAsync(ocr, img.Pixels, img.Width, img.Height);
            foreach (var b in LayoutBuilder.Build(lines, det.RectForNormalization(img.Width, img.Height), rt.Cfg.Tuning, rt.Cfg.Display.FontScale))
            {
                var ns = SourceNormalizer.Normalize(b.Text);
                if (rt.Svc.Lookup(ns.Key) is null && LlmEligibility.IsEligible(ns.Key)) missing[ns.Key] = ns;
            }
        }
        int chars = missing.Keys.Sum(k => k.Length);
        log($"[warm] {missing.Count} câu chưa có bản dịch, dự kiến gửi {chars} ký tự (hôm nay đã dùng {rt.Usage.Used}/{rt.Cfg.Tuning.DailyCapChars}). TỐN TIỀN nếu translator=claude.");
        if (!yes) { log("[warm] chưa chạy: thêm --yes để dịch và ghi cache."); return 1; }
        var n = await rt.Svc.ProcessAsync(missing.Values.ToList(), default);
        rt.Cache.Save();
        log($"[warm] đã dịch {n} câu · gọi backend {rt.Svc.BackendCalls} · HTTP thật {rt.HttpCalls} · ký tự hôm nay {rt.Usage.Used}");
        return 0;
    }

    // ------------------------------------------------------------------ cache-export
    public static Task<int> CacheExportAsync(Args a)
    {
        string outFile = a.Require("out");
        Action<string> log = m => Console.WriteLine(m);
        var rt = Runtime.Create(a, log);
        var sb = new StringBuilder();
        sb.AppendLine("status\tsource\tvi\tshort_vi\tprovider\tattempts\tlast_error");
        foreach (var e in rt.Cache.All.OrderBy(e => e.Status).ThenBy(e => e.SourceKey, StringComparer.Ordinal))
            sb.AppendLine(string.Join('\t', e.Status, Tsv(e.SourceKey), Tsv(e.Target), Tsv(e.ShortVi), e.Provider, e.Attempts, Tsv(e.LastError)));
        foreach (var t in rt.Glossary.Terms)
            sb.AppendLine(string.Join('\t', "glossary", Tsv(t.TermEn), Tsv(t.TermVi), Tsv(t.ShortVi), "glossary", 0, ""));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outFile))!);
        File.WriteAllText(outFile, sb.ToString(), new UTF8Encoding(true));
        log($"[cache-export] {rt.Cache.All.Count} mục cache + {rt.Glossary.Terms.Count} glossary → {outFile}");
        return Task.FromResult(0);
    }

    private static string Tsv(string? s) => (s ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
}
