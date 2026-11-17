using System.Net.Sockets;
using System.Text;

namespace Engine.Protocol;

public interface IFrameSink
{
    /// <summary>Không chặn; gói mới thay gói chưa gửi (mỗi gói là trạng thái đầy đủ).</summary>
    void Send(string line);
}

public sealed class NullSink : IFrameSink
{
    public int Count { get; private set; }
    public void Send(string line) => Count++;
}

/// <summary>Client TCP tới TV: tự kết nối lại mỗi retryMs, hello, gửi bất đồng bộ (không chặn vòng chụp).</summary>
public sealed class TvClient(string host, int port, string token, Action<string>? log = null, int retryMs = 2000) : IFrameSink, IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _signal = new(0, int.MaxValue);
    private string? _pending;
    private volatile bool _connected;
    private volatile string? _rejected;
    private Task? _loop;

    public bool Connected => _connected;

    /// <summary>"token"/"version" khi TV từ chối ⇒ client dừng thử lại.</summary>
    public string? RejectedReason => _rejected;

    public void Start() => _loop ??= Task.Run(RunAsync);

    public void Send(string line)
    {
        Interlocked.Exchange(ref _pending, line);
        try { _signal.Release(); } catch (SemaphoreFullException) { }
    }

    private async Task RunAsync()
    {
        var ct = _cts.Token;
        bool announcedDown = false;
        while (!ct.IsCancellationRequested && _rejected is null)
        {
            try
            {
                using var tcp = new TcpClient { NoDelay = true };
                using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    connectCts.CancelAfter(2000);
                    await tcp.ConnectAsync(host, port, connectCts.Token);
                }
                var stream = tcp.GetStream();
                var reader = new StreamReader(stream, new UTF8Encoding(false));
                await WriteLineAsync(stream, ProtocolJson.SerializeHello(token), ct);

                using (var replyCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    replyCts.CancelAfter(3000);
                    var replyLine = await reader.ReadLineAsync(replyCts.Token);
                    var reply = replyLine is null ? null : ProtocolJson.ParseReply(replyLine);
                    if (reply is null) throw new IOException("TV không trả lời hello hợp lệ");
                    if (!reply.Ok)
                    {
                        _rejected = reply.Reason ?? "unknown";
                        log?.Invoke($"[tv] TV từ chối: {_rejected} — dừng kết nối (kiểm mã ghép cặp: engine pair).");
                        return;
                    }
                }

                _connected = true;
                announcedDown = false;
                log?.Invoke($"[tv] đã kết nối {host}:{port}");
                var eof = reader.ReadLineAsync(ct).AsTask(); // phát hiện TV đóng kết nối
                while (!ct.IsCancellationRequested)
                {
                    var wake = _signal.WaitAsync(ct);
                    var done = await Task.WhenAny(wake, eof);
                    if (done == eof) break; // TV đóng/ lỗi
                    var line = Interlocked.Exchange(ref _pending, null);
                    if (line is not null) await WriteLineAsync(stream, line, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or ObjectDisposedException or InvalidOperationException)
            {
                if (!announcedDown) { log?.Invoke($"[tv] chưa kết nối được {host}:{port} ({ex.GetType().Name}) — thử lại mỗi {retryMs} ms"); announcedDown = true; }
            }
            finally { _connected = false; }

            try { await Task.Delay(retryMs, ct); } catch (OperationCanceledException) { break; }
        }
    }

    private static async Task WriteLineAsync(NetworkStream s, string line, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(line + "\n");
        await s.WriteAsync(bytes, ct);
        await s.FlushAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_loop is not null)
            try { await _loop.WaitAsync(TimeSpan.FromSeconds(2)); } catch (Exception) { /* đang tắt */ }
        _cts.Dispose();
    }

    /// <summary>Một lần kết nối + hello (cho lệnh pair); null khi không kết nối được.</summary>
    public static async Task<HelloReply?> TestHelloAsync(string host, int port, string token, int timeoutMs = 3000)
    {
        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, cts.Token);
            var stream = tcp.GetStream();
            await WriteLineAsync(stream, ProtocolJson.SerializeHello(token), cts.Token);
            var line = await new StreamReader(stream, new UTF8Encoding(false)).ReadLineAsync(cts.Token);
            return line is null ? null : ProtocolJson.ParseReply(line);
        }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException) { return null; }
    }
}
