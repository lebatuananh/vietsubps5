using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Geometry;

namespace Engine.Protocol;

// Nguồn chân lý của giao thức: tv-overlay/README.md mục "Giao thức".

public sealed record CoverSpec(string Mode, string Color);

public sealed record DisplaySpec(int MinFontPx, int HoldMs);

public sealed record OverlayItem(
    string Id, Box Box, string Text, string? Alt, int FontPx, string Group, int MaxLines,
    string Align, bool Bold, string TextColor, CoverSpec Cover, Box? Room);

public sealed record OverlayFrame(
    int V, string SessionId, long Seq, int TtlMs, DisplaySpec? Display, IReadOnlyList<OverlayItem> Items);

public sealed record HelloReply(bool Ok, string? Reason);

public static class ProtocolJson
{
    public const int Version = 1;
    public const int Port = 47820;
    public const int MaxLineBytes = 262144;

    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Một dòng JSON, không chứa '\n'.</summary>
    public static string SerializeFrame(OverlayFrame frame) => JsonSerializer.Serialize(frame, Options);

    /// <summary>null khi không phải JSON/ thiếu trường bắt buộc.</summary>
    public static OverlayFrame? ParseFrame(string line)
    {
        try
        {
            var f = JsonSerializer.Deserialize<OverlayFrame>(line, Options);
            return f is null || f.V == 0 || f.SessionId is null || f.Items is null ? null : f;
        }
        catch (JsonException) { return null; }
    }

    public static string SerializeHello(string token) => JsonSerializer.Serialize(new { hello = new { v = Version, token } }, Options);

    public static HelloReply? ParseReply(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("ok", out var ok)
                || ok.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return null;
            string? reason = doc.RootElement.TryGetProperty("reason", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() : null;
            return new HelloReply(ok.GetBoolean(), reason);
        }
        catch (JsonException) { return null; }
    }

    /// <summary>"#AARRGGBB" hoa.</summary>
    public static string Argb(byte a, byte r, byte g, byte b) => $"#{a:X2}{r:X2}{g:X2}{b:X2}";
}

/// <summary>Giới hạn giao thức v1 (README tv-overlay mục 4): vượt ⇒ TV bỏ cả gói, nên engine tự giữ.</summary>
public static class ProtocolLimits
{
    public const int MaxItems = 200;
    public const int MaxTextChars = 1000;
}
