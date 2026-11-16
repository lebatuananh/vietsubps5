using Engine.Capture;
using Engine.Geometry;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace Engine.Ocr;

public sealed record OcrLineModel(string Text, Box BoxPx, Box Box01);

public sealed record SizeModel(int Width, int Height);

/// <summary>Nội dung <c>&lt;ảnh&gt;.ocr.json</c> — output chẩn đoán của Phase 1, không phải contract với Phase 3.</summary>
public sealed record OcrFileModel(
    string Image,
    SizeModel ImageSize,
    ContentRect ContentRect,
    bool Confident,
    string? Fallback,
    double ScaleApplied,
    string Variant,
    IReadOnlyList<OcrLineModel> Lines);

public static class OcrRunner
{
    public const string LanguageTag = "en-US";

    public static OcrEngine CreateEngine()
    {
        var engine = OcrEngine.TryCreateFromLanguage(new Language(LanguageTag));
        if (engine is null)
            throw new EngineException(5,
                $"Máy không có gói OCR '{LanguageTag}' (OcrEngine.TryCreateFromLanguage trả null). " +
                "Cài: PowerShell admin ⇒ Get-WindowsCapability -Online | Where-Object Name -like 'Language.OCR*en-US*' | Add-WindowsCapability -Online");
        return engine;
    }

    /// <param name="scaleRequested">1 = ảnh gốc; 2 = phóng 2× trước OCR (biến thể tiền xử lý rẻ).</param>
    public static async Task<OcrFileModel> RunAsync(OcrEngine engine, string imagePath, double scaleRequested)
    {
        var original = await ImageIo.ReadBgraAsync(imagePath);
        var detection = ContentRectDetector.Detect(original.Pixels, original.Width, original.Height, original.Stride);
        // NB2: rect không confident thì KHÔNG dùng để chuẩn hoá — dùng cả ảnh.
        var rectUsed = detection.RectForNormalization(original.Width, original.Height);

        // Giới hạn OcrEngine.MaxImageDimension: thu lại nếu phóng/gốc vượt quá.
        double scale = scaleRequested;
        double maxDim = Math.Max(original.Width, original.Height) * scale;
        if (maxDim > OcrEngine.MaxImageDimension)
            scale = (double)OcrEngine.MaxImageDimension / Math.Max(original.Width, original.Height);

        var input = Math.Abs(scale - 1.0) < 1e-9 ? original : await ImageIo.ReadBgraAsync(imagePath, scale);
        double effective = (double)input.Width / original.Width;

        var writer = new DataWriter();
        writer.WriteBytes(input.Pixels);
        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(
            writer.DetachBuffer(), BitmapPixelFormat.Bgra8, input.Width, input.Height, BitmapAlphaMode.Ignore);

        var result = await engine.RecognizeAsync(bitmap);
        var lines = new List<OcrLineModel>();
        foreach (var line in result.Lines)
        {
            double x1 = double.MaxValue, y1 = double.MaxValue, x2 = double.MinValue, y2 = double.MinValue;
            foreach (var w in line.Words)
            {
                var r = w.BoundingRect;
                x1 = Math.Min(x1, r.X); y1 = Math.Min(y1, r.Y);
                x2 = Math.Max(x2, r.X + r.Width); y2 = Math.Max(y2, r.Y + r.Height);
            }
            if (x2 < x1) continue; // dòng không có từ
            // Quy toạ độ về ảnh GỐC (đã cắt client), kể cả khi phóng/ thu.
            var boxPx = new Box(x1 / effective, y1 / effective, (x2 - x1) / effective, (y2 - y1) / effective);
            lines.Add(new OcrLineModel(line.Text, boxPx, Normalizer.Normalize(boxPx, rectUsed)));
        }

        return new OcrFileModel(
            Path.GetFileName(imagePath),
            new SizeModel(original.Width, original.Height),
            rectUsed, detection.Confident, detection.Fallback, effective,
            Math.Abs(scaleRequested - 1.0) < 1e-9 ? "scale1" : $"scale{scaleRequested:0.#}",
            lines);
    }
}
