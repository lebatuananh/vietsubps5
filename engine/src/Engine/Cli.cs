using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine;

/// <summary>Parse <c>--key value</c> thủ công (không dùng thư viện CLI). Cờ không giá trị: <c>--flag</c>.</summary>
public sealed class Args
{
    /// <summary>Cờ boolean: <c>--no-send</c> trần hoặc <c>--no-send true</c> đều được.</summary>
    private static readonly HashSet<string> BoolFlags = new(StringComparer.OrdinalIgnoreCase) { "no-send", "send", "yes", "verbose" };

    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public static Args Parse(IReadOnlyList<string> args, int start)
    {
        var a = new Args();
        for (int i = start; i < args.Count; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Tham số lạ '{args[i]}' (mong đợi --tên giá-trị).");
            var key = args[i][2..];
            if (i + 1 >= args.Count || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                if (BoolFlags.Contains(key)) { a._values[key] = "true"; continue; } // NB11: cờ trần = true
                throw new ArgumentException($"Thiếu giá trị cho --{key}.");
            }
            a._values[key] = args[++i];
        }
        return a;
    }

    public string? Get(string key) => _values.TryGetValue(key, out var v) ? v : null;

    public string Require(string key)
        => Get(key) ?? throw new ArgumentException($"Thiếu tham số bắt buộc --{key}.");

    public int GetInt(string key, int def)
    {
        var v = Get(key);
        if (v is null) return def;
        return int.TryParse(v, out var n) ? n : throw new ArgumentException($"--{key} phải là số nguyên, nhận '{v}'.");
    }

    public double GetDouble(string key, double def)
    {
        var v = Get(key);
        if (v is null) return def;
        return double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n)
            ? n : throw new ArgumentException($"--{key} phải là số, nhận '{v}'.");
    }
}

public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
