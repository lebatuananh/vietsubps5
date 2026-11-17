using System.Text;
using System.Text.Json;

namespace Engine.Translation;

public sealed record ClaudeRequest(string Model, int MaxTokens, string System, string User, string SchemaJson);

public sealed record ClaudeRawResponse(
    string? Text, string? StopReason, long InputTokens, long OutputTokens, long CacheReadTokens, long CacheCreationTokens);

public interface IClaudeTransport
{
    Task<ClaudeRawResponse> SendAsync(ClaudeRequest request, CancellationToken ct);
}

/// <summary>Lỗi từ transport: transient (mạng/429/5xx) ⇒ không tính attempts, lùi 30 s.</summary>
public sealed class ClaudeTransportException(bool transient, string message, bool refundable = false) : Exception(message)
{
    public bool Transient { get; } = transient;
    /// <summary>true ⇒ yêu cầu chắc chắn không bị tính tiền (429/5xx) nên hoàn lại ký tự đã giữ chỗ.</summary>
    public bool Refundable { get; } = refundable;
}

/// <summary>Phần THUẦN của lời gọi LLM: dựng prompt/schema, parse phản hồi. Không I/O.</summary>
public static class ClaudePrompt
{
    public const string Model = "claude-sonnet-5-5";
    public const int MaxTokens = 8000;

    /// <summary>Ổn định tuyệt đối (không giờ/ngẫu nhiên) để prompt caching trúng; hướng dẫn + glossary.</summary>
    public static string BuildSystem(Glossary glossary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là biên dịch viên giao diện game bóng đá EA SPORTS FC 26 (chế độ Career Mode), dịch từ tiếng Anh sang tiếng Việt.");
        sb.AppendLine("Người đọc là người chơi Việt Nam xem chữ trên TV từ xa: bản dịch phải ngắn gọn, tự nhiên, dùng thuật ngữ bóng đá Việt quen thuộc.");
        sb.AppendLine();
        sb.AppendLine("QUY TẮC:");
        sb.AppendLine("1. Giữ NGUYÊN các slot dạng {N1}, {N2}… (chữ N hoa, đúng chỉ số). Mỗi slot của câu gốc phải xuất hiện trong bản dịch, không thêm slot mới; được đổi thứ tự cho đúng ngữ pháp Việt.");
        sb.AppendLine("2. KHÔNG dịch tên riêng: tên cầu thủ, câu lạc bộ, giải đấu, huấn luyện viên, thương hiệu, tên chế độ chơi.");
        sb.AppendLine("3. Xưng hô: dùng \"HLV\" ở nhãn và menu; trong thư/ thông báo trang trọng gửi người chơi dùng \"ông\" (ví dụ \"Ban lãnh đạo mong ông…\"); không dùng \"bạn\".");
        sb.AppendLine("4. Nếu câu gốc viết HOA TOÀN BỘ thì bản dịch cũng viết hoa toàn bộ (giữ dấu tiếng Việt).");
        sb.AppendLine("5. Giữ nguyên dấu câu, số, ký hiệu tiền tệ nằm ngoài slot. Không thêm giải thích.");
        sb.AppendLine("6. Với nhãn dài hơn 12 ký tự, cung cấp thêm \"short_vi\": bản ngắn hơn đáng kể (≤ 70% độ dài) nhưng vẫn đúng nghĩa. Nhãn ngắn thì bỏ \"short_vi\".");
        sb.AppendLine("7. Thuật ngữ trong GLOSSARY dưới đây là bắt buộc khi xuất hiện nguyên câu hoặc là một phần câu.");
        sb.AppendLine();
        sb.AppendLine("Trả về JSON đúng schema: {\"items\":[{\"id\":\"…\",\"vi\":\"…\",\"short_vi\":\"…\"}]} — mỗi id đầu vào đúng một mục.");
        sb.AppendLine();
        sb.AppendLine($"GLOSSARY v{glossary.Version} (tiếng Anh → tiếng Việt):");
        foreach (var t in glossary.Terms)
        {
            sb.Append("- ").Append(t.TermEn).Append(" → ").Append(t.TermVi);
            if (t.DoNotTranslate) sb.Append(" (giữ nguyên)");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>JSON mảng [{"id","en"}].</summary>
    public static string BuildUser(IReadOnlyList<BackendItem> items)
        => JsonSerializer.Serialize(items.Select(i => new { id = i.Id, en = i.En }),
            new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

    /// <summary>Mọi object <c>additionalProperties:false</c>; <c>short_vi</c> không nằm trong <c>required</c> (N10).</summary>
    public static string SchemaJson { get; } =
        "{\"type\":\"object\",\"properties\":{\"items\":{\"type\":\"array\",\"items\":{\"type\":\"object\","
        + "\"properties\":{\"id\":{\"type\":\"string\"},\"vi\":{\"type\":\"string\"},\"short_vi\":{\"type\":\"string\"}},"
        + "\"required\":[\"id\",\"vi\"],\"additionalProperties\":false}}},\"required\":[\"items\"],\"additionalProperties\":false}";

    public static BackendResult ParseResponse(string? text, IReadOnlyList<BackendItem> requested)
    {
        if (string.IsNullOrWhiteSpace(text)) return new(BackendOutcome.Dropped, [], "empty");
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("items", out var arr)
                || arr.ValueKind != JsonValueKind.Array) return new(BackendOutcome.Dropped, [], "no items");
            var known = requested.Select(r => r.Id).ToHashSet();
            var list = new List<BackendTranslated>();
            foreach (var el in arr.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty("id", out var id) || !el.TryGetProperty("vi", out var vi)) continue;
                var idS = id.GetString();
                if (idS is null || !known.Contains(idS)) continue;
                string? sv = el.TryGetProperty("short_vi", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
                list.Add(new BackendTranslated(idS, vi.GetString() ?? "", string.IsNullOrWhiteSpace(sv) ? null : sv));
            }
            return new(BackendOutcome.Ok, list, null);
        }
        catch (JsonException ex) { return new(BackendOutcome.Dropped, [], "parse: " + ex.Message); }
    }
}

public sealed class ClaudeBackend(IClaudeTransport transport, Glossary glossary, Action<string>? log = null) : ITranslationBackend
{
    public long CacheReadTokens { get; private set; }
    public long InputTokens { get; private set; }
    public long OutputTokens { get; private set; }

    public async Task<BackendResult> TranslateAsync(IReadOnlyList<BackendItem> items, CancellationToken ct)
    {
        var req = new ClaudeRequest(ClaudePrompt.Model, ClaudePrompt.MaxTokens, ClaudePrompt.BuildSystem(glossary),
            ClaudePrompt.BuildUser(items), ClaudePrompt.SchemaJson);
        ClaudeRawResponse resp;
        try { resp = await transport.SendAsync(req, ct); }
        catch (ClaudeTransportException ex)
        {
            log?.Invoke($"[llm] lỗi {(ex.Transient ? "tạm thời" : "không thử lại được")}: {ex.Message}");
            // Lỗi không thể thử lại (400/401/403/404…) là lỗi CẤU HÌNH, không phải lỗi đầu ra mô hình ⇒ Fatal (B2), không phải "lô bị bỏ".
            return ex.Transient ? new(BackendOutcome.Transient, [], ex.Message, ex.Refundable) : new(BackendOutcome.Fatal, [], ex.Message);
        }

        InputTokens += resp.InputTokens; OutputTokens += resp.OutputTokens; CacheReadTokens += resp.CacheReadTokens;
        log?.Invoke($"[llm] lô {items.Count} câu: in={resp.InputTokens} out={resp.OutputTokens} cache_read={resp.CacheReadTokens} cache_create={resp.CacheCreationTokens} stop={resp.StopReason}");
        if (resp.StopReason is "refusal" or "max_tokens")
            return new(BackendOutcome.Dropped, [], resp.StopReason);
        return ClaudePrompt.ParseResponse(resp.Text, items);
    }
}
