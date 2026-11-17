using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Engine.Protocol;
using Engine.Translation;

namespace Engine.Tests;

public class ClaudePromptTests
{
    private static readonly Glossary G = new() { Version = 3, Terms = [new GlossaryTerm("Inbox", "Hộp thư", "Thư", "ui"), new GlossaryTerm("Scout Report", "Báo cáo tuyển trạch", null, "ui")] };

    [Fact]
    public void System_IsDeterministic_AndContainsGlossaryAndRules()
    {
        var a = ClaudePrompt.BuildSystem(G);
        Assert.Equal(a, ClaudePrompt.BuildSystem(G));
        Assert.Contains("Hộp thư", a);
        Assert.Contains("Báo cáo tuyển trạch", a);
        Assert.Contains("{N1}", a);                                    // luật slot
        Assert.Contains("HLV", a);                                     // xưng hô mặc định
        Assert.DoesNotContain(DateTime.UtcNow.Year.ToString() + "-", a); // không nhúng thời gian
    }

    [Fact]
    public void Schema_AllObjectsClosed_ShortViOptional()
    {
        using var doc = JsonDocument.Parse(ClaudePrompt.SchemaJson);
        void Walk(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                if (e.TryGetProperty("type", out var ty) && ty.GetString() == "object")
                    Assert.False(e.GetProperty("additionalProperties").GetBoolean());
                foreach (var p in e.EnumerateObject()) Walk(p.Value);
            }
            else if (e.ValueKind == JsonValueKind.Array) foreach (var x in e.EnumerateArray()) Walk(x);
        }
        Walk(doc.RootElement);
        var item = doc.RootElement.GetProperty("properties").GetProperty("items").GetProperty("items");
        var required = item.GetProperty("required").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("id", required); Assert.Contains("vi", required);
        Assert.DoesNotContain("short_vi", required);
        Assert.True(item.GetProperty("properties").TryGetProperty("short_vi", out _));
    }

    [Fact]
    public void User_IsJsonArrayWithIds()
    {
        var u = ClaudePrompt.BuildUser([new BackendItem("s1", "Scout Report"), new BackendItem("s2", "Budget: {N1}")]);
        using var doc = JsonDocument.Parse(u);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
        Assert.Equal("s2", doc.RootElement[1].GetProperty("id").GetString());
        Assert.Equal("Budget: {N1}", doc.RootElement[1].GetProperty("en").GetString());
    }

    private static readonly BackendItem[] Req = [new("s1", "A"), new("s2", "B")];

    [Fact]
    public void ParseResponse_Ok_IgnoresUnknownIds()
    {
        var r = ClaudePrompt.ParseResponse("{\"items\":[{\"id\":\"s1\",\"vi\":\"Một\",\"short_vi\":\"M\"},{\"id\":\"zzz\",\"vi\":\"x\"},{\"id\":\"s2\",\"vi\":\"Hai\"}]}", Req);
        Assert.Equal(BackendOutcome.Ok, r.Outcome);
        Assert.Equal(2, r.Items.Count);
        Assert.Equal("M", r.Items[0].ShortVi);
        Assert.Null(r.Items[1].ShortVi);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"foo\":1}")]
    public void ParseResponse_Invalid_IsDropped(string? text)
        => Assert.Equal(BackendOutcome.Dropped, ClaudePrompt.ParseResponse(text, Req).Outcome);
}

public class ClaudeBackendTests
{
    private sealed class FakeTransport(Func<ClaudeRequest, ClaudeRawResponse> h) : IClaudeTransport
    {
        public List<ClaudeRequest> Requests { get; } = new();
        public Task<ClaudeRawResponse> SendAsync(ClaudeRequest r, CancellationToken ct) { Requests.Add(r); return Task.FromResult(h(r)); }
    }

    private static readonly Glossary G = new() { Version = 1, Terms = [] };
    private static readonly BackendItem[] Req = [new("s1", "Scout Report")];

    [Fact]
    public async Task Ok_BuildsRequestAsSpecified_AndReadsUsage()
    {
        var tr = new FakeTransport(_ => new ClaudeRawResponse("{\"items\":[{\"id\":\"s1\",\"vi\":\"Báo cáo\"}]}", "end_turn", 100, 20, 80, 0));
        var be = new ClaudeBackend(tr, G);
        var r = await be.TranslateAsync(Req, default);
        Assert.Equal(BackendOutcome.Ok, r.Outcome);
        Assert.Equal("Báo cáo", r.Items[0].Vi);
        var req = Assert.Single(tr.Requests);
        Assert.Equal("claude-sonnet-5-5", req.Model);
        Assert.Equal(8000, req.MaxTokens);
        Assert.Equal(ClaudePrompt.BuildSystem(G), req.System);
        Assert.Equal(ClaudePrompt.SchemaJson, req.SchemaJson);
        Assert.Equal(80, be.CacheReadTokens);
    }

    [Theory]
    [InlineData("refusal")]
    [InlineData("max_tokens")]
    public async Task RefusalOrMaxTokens_DropsTheBatch(string stop)
    {
        var tr = new FakeTransport(_ => new ClaudeRawResponse("{\"items\":[]}", stop, 1, 1, 0, 0));
        Assert.Equal(BackendOutcome.Dropped, (await new ClaudeBackend(tr, G).TranslateAsync(Req, default)).Outcome);
    }

    private sealed class Throwing(bool transient) : IClaudeTransport
    {
        public Task<ClaudeRawResponse> SendAsync(ClaudeRequest r, CancellationToken ct) => throw new ClaudeTransportException(transient, "boom");
    }

    [Fact]
    public async Task TransientTransportError_IsTransient_Others_AreFatal()
    {
        Assert.Equal(BackendOutcome.Transient, (await new ClaudeBackend(new Throwing(true), G).TranslateAsync(Req, default)).Outcome);
        Assert.Equal(BackendOutcome.Fatal, (await new ClaudeBackend(new Throwing(false), G).TranslateAsync(Req, default)).Outcome);
    }
}

public class TvClientTests
{
    private static (TcpListener l, int port) Listen()
    {
        var l = new TcpListener(IPAddress.Loopback, 0); l.Start();
        return (l, ((IPEndPoint)l.LocalEndpoint).Port);
    }

    private static async Task<string?> ReadLine(NetworkStream s, int timeoutMs = 3000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        var sb = new StringBuilder(); var buf = new byte[1];
        while (true)
        {
            int n = await s.ReadAsync(buf, cts.Token);
            if (n == 0) return null;
            if (buf[0] == '\n') return sb.ToString();
            sb.Append((char)buf[0]);
        }
    }

    private static async Task Write(NetworkStream s, string line) => await s.WriteAsync(Encoding.UTF8.GetBytes(line + "\n"));

    [Fact]
    public async Task Hello_ThenFrame_Delivered()
    {
        var (l, port) = Listen();
        await using var c = new TvClient("127.0.0.1", port, "ACDEFHJK", retryMs: 100);
        c.Start();
        using var sock = await l.AcceptTcpClientAsync();
        var s = sock.GetStream();
        Assert.Equal("{\"hello\":{\"v\":1,\"token\":\"ACDEFHJK\"}}", await ReadLine(s));
        await Write(s, "{\"ok\":true}");
        c.Send("{\"v\":1,\"x\":1}");
        Assert.Equal("{\"v\":1,\"x\":1}", await ReadLine(s));
        Assert.True(c.Connected);
        l.Stop();
    }

    [Fact]
    public async Task WrongToken_IsRejected_AndClientStopsRetrying()
    {
        var (l, port) = Listen();
        await using var c = new TvClient("127.0.0.1", port, "WRONG234", retryMs: 100);
        c.Start();
        using var sock = await l.AcceptTcpClientAsync();
        var s = sock.GetStream();
        await ReadLine(s);
        await Write(s, "{\"ok\":false,\"reason\":\"token\"}");
        sock.Close();
        await Task.Delay(500);
        Assert.Equal("token", c.RejectedReason);
        Assert.False(l.Pending()); // không kết nối lại
        l.Stop();
    }

    [Fact]
    public async Task Reconnects_AfterServerDropsConnection()
    {
        var (l, port) = Listen();
        await using var c = new TvClient("127.0.0.1", port, "ACDEFHJK", retryMs: 100);
        c.Start();
        var first = await l.AcceptTcpClientAsync();
        await ReadLine(first.GetStream());
        await Write(first.GetStream(), "{\"ok\":true}");
        first.Close();
        using var second = await l.AcceptTcpClientAsync().WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal("{\"hello\":{\"v\":1,\"token\":\"ACDEFHJK\"}}", await ReadLine(second.GetStream()));
        l.Stop();
    }

    [Fact]
    public async Task Send_DoesNotBlock_WhenNoServer()
    {
        await using var c = new TvClient("127.0.0.1", 1, "ACDEFHJK", retryMs: 100); // cổng 1: từ chối
        c.Start();
        var t0 = DateTime.UtcNow;
        for (int i = 0; i < 1000; i++) c.Send("{\"v\":1}");
        Assert.True((DateTime.UtcNow - t0).TotalMilliseconds < 500);
        Assert.False(c.Connected);
    }

    [Fact]
    public async Task TestHello_ReturnsReply_OrNullWhenUnreachable()
    {
        var (l, port) = Listen();
        var t = TvClient.TestHelloAsync("127.0.0.1", port, "ACDEFHJK", 2000);
        using var sock = await l.AcceptTcpClientAsync();
        await ReadLine(sock.GetStream());
        await Write(sock.GetStream(), "{\"ok\":false,\"reason\":\"version\"}");
        var r = await t;
        Assert.False(r!.Ok); Assert.Equal("version", r.Reason);
        l.Stop();
        Assert.Null(await TvClient.TestHelloAsync("127.0.0.1", port, "ACDEFHJK", 500));
    }
}
