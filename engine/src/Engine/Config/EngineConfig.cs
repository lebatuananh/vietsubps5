using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Config;

/// <summary>Thư mục gốc dữ liệu: biến môi trường VIETSUB_HOME, mặc định %LOCALAPPDATA%\VietSub (Rev 1.1 N11).</summary>
public static class VietSubHome
{
    public static string Resolve(Func<string, string?>? env = null)
    {
        env ??= Environment.GetEnvironmentVariable;
        var v = env("VIETSUB_HOME");
        return !string.IsNullOrWhiteSpace(v)
            ? v
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VietSub");
    }
}

public sealed class TvConfig
{
    public string? Host { get; set; }
    public int Port { get; set; } = 47820;
    public string? Token { get; set; }
}

public sealed class DisplayConfig
{
    public string CoverMode { get; set; } = "auto";
    public int MinFontPx { get; set; } = 24;
    public int HoldMs { get; set; } = 0;
    public double FontScale { get; set; } = 1.0; // Rev 1.3: 0,78 làm chữ Việt chỉ ~56% chữ gốc
}

/// <summary>Mọi ngưỡng đọc từ engine.local.json khối "tuning"; mặc định = hằng số có tên ở đây.</summary>
public sealed class TuningConfig
{
    public double CellDiffThreshold { get; set; } = 6;
    public int StableMs { get; set; } = 250;
    public int FrameIntervalMs { get; set; } = 250;
    public int HeartbeatMs { get; set; } = 1000;
    public int TtlMs { get; set; } = 3000;
    public int BackgroundCellMs { get; set; } = 3000;
    public double MatchModeCellRatio { get; set; } = 0.5;
    public int MatchModeWindowMs { get; set; } = 3000;
    public int MatchModeCalmMs { get; set; } = 1500;
    public double SplitGapFactor { get; set; } = 1.2;
    public double MergeLeftTolerance { get; set; } = 0.006;
    public double MergeHeightTolerance { get; set; } = 0.35; // review 3a NB8: 0,20 làm đứt đoạn email (hộp OCR 23 px vs 29 px)
    public double MergeGapFactor { get; set; } = 0.9;
    public double GroupHeightTolerance { get; set; } = 0.30; // doc D6 ghi ±10%; đo trên ảnh tổng hợp: hộp OCR dao động tới ~25% theo nét chữ
    public double RowCenterTolerance { get; set; } = 0.005;
    public double RowLabelGapMax { get; set; } = 0.08;
    public int SampleRimPx { get; set; } = 3;
    public double CoverUniformThreshold { get; set; } = 18;
    public double RoomMaxFactor { get; set; } = 2.0;
    public int BatchMax { get; set; } = 40;
    public int BackoffMs { get; set; } = 30000;
    public int MaxAttempts { get; set; } = 2;
    public int DailyCapChars { get; set; } = 100000;
    public int BoxPadPx { get; set; } = 3;
    public int MaxItemsPerFrame { get; set; } = 200;
    public int MatchModeMinBeats { get; set; } = 3;
    public int MatchModeSustainMs { get; set; } = 3000;
}

public sealed class EngineConfig
{
    public TvConfig Tv { get; set; } = new();
    public DisplayConfig Display { get; set; } = new();
    public TuningConfig Tuning { get; set; } = new();

    /// <summary>NB12: giá trị lạ không được đi thẳng ra dây TV.</summary>
    public void Validate()
    {
        var modes = new[] { "auto", "solid", "card", "none" };
        if (!modes.Contains(Display.CoverMode?.ToLowerInvariant()))
            throw new ArgumentException($"engine.local.json: display.coverMode phải là auto|solid|card|none, nhận '{Display.CoverMode}'.");
        if (Display.MinFontPx < 1 || Display.FontScale <= 0)
            throw new ArgumentException("engine.local.json: display.minFontPx ≥ 1 và display.fontScale > 0.");
    }

    public static string PathIn(string home) => Path.Combine(home, "engine.local.json");

    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static EngineConfig Load(string home)
    {
        var p = PathIn(home);
        if (!File.Exists(p)) return new EngineConfig();
        var cfg = JsonSerializer.Deserialize<EngineConfig>(File.ReadAllText(p), Opts) ?? new EngineConfig();
        cfg.Validate();
        return cfg;
    }

    public void Save(string home)
    {
        Directory.CreateDirectory(home);
        var p = PathIn(home);
        var tmp = p + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, Opts));
        File.Move(tmp, p, overwrite: true);
    }
}

public static class PairCode
{
    public const string Alphabet = "ACDEFHJKMNPRTUVWXY3479";

    /// <summary>Hoa, bỏ '-' và khoảng trắng.</summary>
    public static string Normalize(string raw) => new string(raw.Where(c => c != '-' && !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();

    public static bool IsValid(string raw) { var n = Normalize(raw); return n.Length == 8 && n.All(c => Alphabet.Contains(c)); }
}
