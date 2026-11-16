using System.Globalization;
using System.Text.Json;
using Engine.Capture;
using Engine.Geometry;
using Engine.Ocr;

namespace Engine;

public sealed record CaptureWindowModel(string Hwnd, string Title, int Width, int Height);
public sealed record CaptureFrameModel(string File, DateTime CapturedAtUtc, double MeanLuma);
public sealed record PointModel(int X, int Y);

/// <summary>Nội dung <c>capture.json</c> — output chẩn đoán của Phase 1, không phải contract với Phase 3.</summary>
public sealed record CaptureJson(
    CaptureWindowModel Window,
    bool DpiPerMonitorV2,
    SizeModel WgcFrameSize,
    ContentRect ClientRect,
    PointModel ClientOffset,
    ContentRect ContentRect,
    bool Confident,
    string? Fallback,
    double MeanLuma,
    IReadOnlyList<CaptureFrameModel> Frames);

public static class Commands
{
    // ------------------------------------------------------------ capture
    public static async Task<int> CaptureAsync(Args a, bool dpiOk)
    {
        string outDir = a.Require("out");
        int frames = a.GetInt("frames", 1);
        int intervalMs = a.GetInt("interval-ms", 500);
        int timeoutMs = a.GetInt("timeout-ms", 5000);
        if (frames < 1) throw new ArgumentException("--frames phải ≥ 1.");

        WindowInfo win;
        var hwndArg = a.Get("hwnd");
        if (hwndArg is not null)
        {
            bool hex = hwndArg.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            long h;
            if (!(hex ? long.TryParse(hwndArg[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out h)
                      : long.TryParse(hwndArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out h)))
                throw new ArgumentException($"--hwnd không hợp lệ: '{hwndArg}' (mong đợi số hoặc 0xHEX).");
            win = WindowFinder.FromHwnd(new IntPtr(h))
                  ?? throw new EngineException(2, $"HWND {hwndArg} không phải cửa sổ hợp lệ.");
        }
        else
        {
            var title = a.Require("window-title");
            var matches = WindowFinder.FindByTitle(title);
            if (matches.Count == 0)
                throw new EngineException(2, $"Không tìm thấy cửa sổ nhìn thấy nào có tiêu đề chứa \"{title}\".");
            if (matches.Count > 1)
                throw new EngineException(2,
                    $"{matches.Count} cửa sổ khớp \"{title}\" — dùng --hwnd để chỉ định:\n  " +
                    string.Join("\n  ", matches.Select(m => m.ToString())));
            win = matches[0];
        }

        Console.WriteLine($"[capture] cửa sổ: {win}");
        Console.WriteLine($"[capture] frames={frames} interval={intervalMs}ms timeout={timeoutMs}ms out={outDir}");

        var captured = await Task.Run(() => WindowCapturer.Capture(win, frames, intervalMs, timeoutMs, cropClient: a.Get("no-crop") is not "true"));

        Directory.CreateDirectory(outDir);
        var models = new List<CaptureFrameModel>();
        for (int i = 0; i < captured.Count; i++)
        {
            var f = captured[i];
            string name = $"frame-{i + 1:000}.png";
            await ImageIo.WritePngAsync(Path.Combine(outDir, name), f.Pixels, f.Width, f.Height);
            double luma = MeanLuma(f.Pixels);
            models.Add(new CaptureFrameModel(name, f.CapturedAtUtc, luma));
            Console.WriteLine($"[capture] {name}: {f.Width}x{f.Height} (WGC {f.WgcWidth}x{f.WgcHeight}, client trong khung {f.ClientRectInFrame}) meanLuma={luma:0.0}");
        }

        var first = captured[0];
        var det = ContentRectDetector.Detect(first.Pixels, first.Width, first.Height, first.Width * 4);
        var json = new CaptureJson(
            new CaptureWindowModel($"0x{win.Hwnd.ToInt64():X}", win.Title, win.Width, win.Height),
            dpiOk,
            new SizeModel(first.WgcWidth, first.WgcHeight),
            first.ClientRectInFrame,
            new PointModel(first.ClientOffsetX, first.ClientOffsetY),
            det.Rect, det.Confident, det.Fallback,
            models.Average(m => m.MeanLuma),
            models);
        await File.WriteAllTextAsync(Path.Combine(outDir, "capture.json"), JsonSerializer.Serialize(json, Json.Options));
        Console.WriteLine($"[capture] contentRect={det.Rect} confident={det.Confident} fallback={det.Fallback ?? "none"} → {Path.Combine(outDir, "capture.json")}");
        return 0;
    }

    private static double MeanLuma(byte[] bgra)
    {
        long sum = 0; long n = 0;
        for (int i = 0; i + 3 < bgra.Length; i += 16) // mỗi 4 điểm ảnh một mẫu
        {
            sum += (18 * bgra[i] + 183 * bgra[i + 1] + 54 * bgra[i + 2]) >> 8;
            n++;
        }
        return n == 0 ? 0 : (double)sum / n;
    }

    // ------------------------------------------------------------ ocr
    public static async Task<int> OcrAsync(Args a)
    {
        string input = a.Require("input");
        string outDir = a.Require("out");
        double scale = a.GetDouble("scale", 1.0);
        if (!Directory.Exists(input)) throw new EngineException(1, $"Thư mục --input không tồn tại: {input}");
        var files = Directory.GetFiles(input, "*.png").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        if (files.Count == 0) throw new EngineException(1, $"Không có file .png trong {input}");

        var engine = OcrRunner.CreateEngine(); // null ⇒ EngineException exit 5
        Directory.CreateDirectory(outDir);
        foreach (var f in files)
        {
            var model = await OcrRunner.RunAsync(engine, f, scale);
            string outPath = Path.Combine(outDir, Path.GetFileNameWithoutExtension(f) + ".ocr.json");
            await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(model, Json.Options));
            Console.WriteLine($"[ocr] {Path.GetFileName(f)} {model.ImageSize.Width}x{model.ImageSize.Height} scaleApplied={model.ScaleApplied:0.###} " +
                              $"content={model.ContentRect} confident={model.Confident} lines={model.Lines.Count} → {outPath}");
        }
        return 0;
    }

    // ------------------------------------------------------------ ocr-score
    public static async Task<int> OcrScoreAsync(Args a)
    {
        string ocrDir = a.Require("ocr");
        string goldenDir = a.Require("golden");
        if (!Directory.Exists(ocrDir)) throw new EngineException(1, $"Thư mục --ocr không tồn tại: {ocrDir}");
        if (!Directory.Exists(goldenDir)) throw new EngineException(1, $"Thư mục --golden không tồn tại: {goldenDir}");
        var ocrFiles = Directory.GetFiles(ocrDir, "*.ocr.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        if (ocrFiles.Count == 0) throw new EngineException(1, $"Không có *.ocr.json trong {ocrDir}");

        int tSeg = 0, tSegOk = 0, tWords = 0, tWordsOk = 0, scored = 0;
        var skipped = new List<string>();
        foreach (var f in ocrFiles)
        {
            string stem = Path.GetFileName(f)[..^".ocr.json".Length];
            string goldenPath = Path.Combine(goldenDir, stem + ".golden.json");
            if (!File.Exists(goldenPath)) { skipped.Add(stem); continue; }

            var golden = JsonSerializer.Deserialize<List<string>>(await File.ReadAllTextAsync(goldenPath))
                         ?? throw new EngineException(1, $"Golden không đọc được: {goldenPath}");
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(f));
            var lines = doc.RootElement.GetProperty("lines").EnumerateArray()
                .Select(l => l.GetProperty("text").GetString() ?? "").ToList();

            var r = OcrScorer.Score(golden, lines);
            scored++;
            Console.WriteLine($"[score] {stem}: đoạn {r.SegmentsMatched}/{r.Segments} (a)={Pct(r.SegmentRate)} · từ {r.WordsMatched}/{r.Words} (b)={Pct(r.WordRate)}");
            foreach (var u in r.Unmatched)
                Console.WriteLine($"    KHÔNG KHỚP: \"{u.Golden}\"  ⇐ dòng OCR gần nhất: \"{u.NearestOcrLine ?? "(không có dòng OCR)"}\"");
            tSeg += r.Segments; tSegOk += r.SegmentsMatched; tWords += r.Words; tWordsOk += r.WordsMatched;
        }
        foreach (var s in skipped) Console.WriteLine($"[score] bỏ qua {s}: không có {s}.golden.json");
        if (scored == 0) throw new EngineException(1, "Không ảnh nào có golden tương ứng — không có gì để chấm.");

        double? a_ = tSeg == 0 ? null : (double)tSegOk / tSeg;
        double? b_ = tWords == 0 ? null : (double)tWordsOk / tWords;
        Console.WriteLine($"[score] TỔNG {scored} ảnh: đoạn {tSegOk}/{tSeg} (a)={Pct(a_)} · từ {tWordsOk}/{tWords} (b)={Pct(b_)}");
        Console.WriteLine($"[score] ngưỡng tham khảo (a) ≥ 95%: {(a_ is >= 0.95 ? "ĐẠT" : "KHÔNG ĐẠT")}");
        return 0;
    }

    private static string Pct(double? v) => v is null ? "không xác định" : $"{v.Value * 100:0.0}%";
}
