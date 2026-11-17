using Engine.Layout;
using Engine.Ocr;
using Engine.Translation;
using Windows.Media.Ocr;

namespace Engine.Pipeline;

/// <summary>Đường dẫn dữ liệu seed trong repo (copy ra output) hoặc leo thư mục tìm engine/data.</summary>
public static class DataPaths
{
    public static string Fc26(string file)
    {
        var inOut = Path.Combine(AppContext.BaseDirectory, "data", "fc26", file);
        if (File.Exists(inOut)) return inOut;
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            var p = Path.Combine(d.FullName, "engine", "data", "fc26", file);
            if (File.Exists(p)) return p;
        }
        throw new FileNotFoundException($"Không thấy data/fc26/{file} (seed trong repo).");
    }
}

public sealed class WindowsOcrProvider(OcrEngine engine) : IOcrProvider
{
    public Task<IReadOnlyList<OcrLineData>> RecognizeAsync(SourceFrame frame, CancellationToken ct)
        => OcrRunner.RecognizeLinesAsync(engine, frame.Bgra, frame.Width, frame.Height);
}

/// <summary>Dịch giả lập cho test/smoke: "[VI] " + gốc (slot giữ nguyên). KHÔNG gọi mạng.</summary>
public sealed class FakeTranslator : ITranslationBackend
{
    public int Calls { get; private set; }

    public bool IsFake => true;
    public string Provider => "fake";

    public Task<BackendResult> TranslateAsync(IReadOnlyList<BackendItem> items, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(new BackendResult(BackendOutcome.Ok,
            items.Select(i => new BackendTranslated(i.Id, "[VI] " + i.En, null)).ToList(), null));
    }
}

/// <summary>Đọc thư mục PNG làm nguồn khung: mỗi ảnh giữ <c>holdMs</c> ở nhịp <c>intervalMs</c> (đồng hồ ảo).</summary>
public sealed class PngDirSource
{
    private readonly List<string> _files;
    private readonly int _intervalMs, _holdMs;

    public PngDirSource(string dir, int intervalMs, int holdMs)
    {
        _files = Directory.GetFiles(dir, "*.png").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
        _intervalMs = intervalMs; _holdMs = holdMs;
    }

    public IReadOnlyList<string> Files => _files;

    public async IAsyncEnumerable<(string file, IReadOnlyList<SourceFrame> frames)> ReadAsync()
    {
        long t = 0;
        foreach (var f in _files)
        {
            var img = await ImageIo.ReadBgraAsync(f);
            var list = new List<SourceFrame>();
            for (int ms = 0; ms <= _holdMs; ms += _intervalMs, t += _intervalMs)
                list.Add(new SourceFrame(img.Pixels, img.Width, img.Height, t, Path.GetFileName(f)));
            yield return (f, list);
        }
    }
}
