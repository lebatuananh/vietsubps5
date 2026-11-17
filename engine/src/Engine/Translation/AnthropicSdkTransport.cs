using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;

namespace Engine.Translation;

/// <summary>
/// Transport thật qua gói NuGet chính thức <c>Anthropic</c> (Apache-2.0). Khoá chỉ từ biến môi trường
/// ANTHROPIC_API_KEY, KHÔNG bao giờ log/ghi file. Không dùng trong test/smoke của lượt 3a (0 HTTP).
/// </summary>
public sealed class AnthropicSdkTransport : IClaudeTransport
{
    private readonly AnthropicClient _client;

    // NB2: MaxRetries = 0 — SDK mặc định tự thử lại 2 lần (gồm timeout) ngoài vòng lùi 30 s của engine ⇒ một lần giữ chỗ có thể thành 3 request.
    public AnthropicSdkTransport(string apiKey) => _client = new AnthropicClient { ApiKey = apiKey, MaxRetries = 0 };

    /// <summary>
    /// Phân loại theo mã HTTP: 429/5xx = tạm thời + KHÔNG bị tính tiền (hoàn ký tự); 408/409 = tạm thời (giữ ký tự);
    /// còn lại 4xx (400/401/403/404…) = lỗi cấu hình ⇒ không tạm thời (B2: tắt backend cả phiên).
    /// </summary>
    public static (bool transient, bool refundable) Classify(int status)
        => status is 429 || status >= 500 ? (true, true)
         : status is 408 or 409 ? (true, false)
         : (false, false);

    public async Task<ClaudeRawResponse> SendAsync(ClaudeRequest r, CancellationToken ct)
    {
        try
        {
            var schema = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(r.SchemaJson)!;
            var resp = await _client.Messages.Create(new MessageCreateParams
            {
                Model = r.Model,
                MaxTokens = r.MaxTokens,
                // N10: system (hướng dẫn + glossary) ổn định; CacheControl ở khối cuối của system.
                System = new List<TextBlockParam> { new() { Text = r.System, CacheControl = new CacheControlEphemeral() } },
                Messages = [new UserMessageParam { Content = r.User }],
                OutputConfig = new OutputConfig
                {
                    Effort = Effort.Low,
                    Format = new JsonOutputFormat { Schema = schema },
                },
            }, ct);

            var text = string.Concat(resp.Content.Select(b => b.Value).OfType<TextBlock>().Select(b => b.Text));
            return new ClaudeRawResponse(text, resp.StopReason?.ToString(), resp.Usage.InputTokens, resp.Usage.OutputTokens,
                resp.Usage.CacheReadInputTokens ?? 0, resp.Usage.CacheCreationInputTokens ?? 0);
        }
        catch (AnthropicRateLimitException ex) { throw new ClaudeTransportException(true, "429: " + ex.Message, refundable: true); }
        catch (Anthropic5xxException ex) { throw new ClaudeTransportException(true, "5xx: " + ex.Message, refundable: true); }
        catch (AnthropicIOException ex) { throw new ClaudeTransportException(true, "mạng: " + ex.Message); } // timeout có thể đã tính tiền ⇒ không hoàn
        catch (AnthropicApiException ex)
        {
            var (transient, refundable) = Classify((int)ex.StatusCode);
            throw new ClaudeTransportException(transient, $"API {(int)ex.StatusCode}: {ex.Message}", refundable);
        }
    }
}
