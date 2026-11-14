#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false
// Xem các lượt worker (SA / Dev / Reviewer) đang chạy hoặc đã chạy — đọc file stream-json ở project-docs/*/_workers/streams/*.jsonl.
// Chạy từ root repo:   dotnet run project-docs/automation/scripts/worker-viewer.cs            (mặc định http://127.0.0.1:4789)
//                      dotnet run project-docs/automation/scripts/worker-viewer.cs -- 4790    (đổi cổng)
// CHỈ ĐỌC file, chỉ nghe 127.0.0.1. Không resume / không gửi gì cho worker.
using System.Text;
using System.Text.Json;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 4789;
var root = FindRepoRoot(Directory.GetCurrentDirectory())
    ?? throw new InvalidOperationException("Không tìm thấy thư mục project-docs — chạy lệnh từ root repo.");
var docs = Path.Combine(root, "project-docs");
// Transcript các phiên Claude Code của chính repo này (phiên Leader): ~/.claude/projects/<root với ':' '\' '/' thành '-'>/*.jsonl
var transcripts = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects",
    root.Replace(':', '-').Replace('\\', '-').Replace('/', '-'));
const long LeaderTailBytes = 3 * 1024 * 1024; // transcript Leader rất lớn ⇒ lần mở đầu chỉ tải 3 MB cuối
var touchedCache = new Dictionary<string, (long Offset, Dictionary<string, int> Files)>(StringComparer.OrdinalIgnoreCase);

var builder = WebApplication.CreateSlimBuilder();
builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
builder.Logging.SetMinimumLevel(LogLevel.Warning);
var app = builder.Build();

// Chống DNS rebinding: chỉ nhận Host của chính máy (trang web lạ trỏ tên miền về 127.0.0.1 sẽ bị 403).
app.Use(async (ctx, next) =>
{
    var host = ctx.Request.Host.Host;
    if (host is not ("127.0.0.1" or "localhost")) { ctx.Response.StatusCode = 403; return; }
    await next();
});

app.MapGet("/", () => Results.Content(Html.Page, "text/html; charset=utf-8"));

List<StreamInfo> AllStreams()
{
    var workers = Directory.EnumerateFiles(docs, "*.jsonl", SearchOption.AllDirectories)
        .Where(f => f.Replace('\\', '/').Contains("/_workers/streams/"))
        .Select(f => Describe(f))
        .ToList();
    var workerSessions = workers.Select(w => w.session).Where(x => x is not null).ToHashSet();
    var leaders = Directory.Exists(transcripts)
        ? Directory.EnumerateFiles(transcripts, "*.jsonl", SearchOption.TopDirectoryOnly)
            .Where(f => !workerSessions.Contains(Path.GetFileNameWithoutExtension(f)))
            .Select(DescribeLeader)
        : [];
    return workers.Concat(leaders).OrderByDescending(x => x.modified).ToList();
}

app.MapGet("/api/streams", () => Results.Json(AllStreams()));

app.MapGet("/api/events", (string id, long from) =>
{
    var file = Resolve(id);
    if (file is null) return Results.NotFound();
    var isLeader = IsTranscript(file);
    var len = new FileInfo(file).Length;
    if (from == 0 && isLeader && len > LeaderTailBytes) from = len - LeaderTailBytes; // dòng đầu bị cắt dở ⇒ TryParse bỏ qua
    var (events, next) = ReadEvents(file, from);
    var running = isLeader ? DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < TimeSpan.FromMinutes(2) : IsRunning(file);
    return Results.Json(new { events, next, running });
});

// Chi tiết một lượt worker: brief, tài liệu được nhắc tới (brief + câu trả lời cuối + Phase doc), file đã sửa.
app.MapGet("/api/detail", (string id) =>
{
    var file = Resolve(id);
    if (file is null || IsTranscript(file)) return Results.NotFound();
    var batchDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(file)!)!)!;
    var name = Path.GetFileNameWithoutExtension(file).Replace("-stream", "");
    var briefFile = Path.Combine(batchDir, "_workers", "briefs", name + "-brief.txt");
    var brief = File.Exists(briefFile) ? File.ReadAllText(briefFile) : null;
    var result = "";
    foreach (var line in Tail(file).Split('\n').Reverse())
        if (TryParse(line) is { } r && Str(r, "type") == "result") { result = Str(r, "result"); break; }
    var refs = new List<string>();
    foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches((brief ?? "") + "\n" + result, @"[A-Za-z0-9_.\-/\\]+\.md"))
        foreach (var baseDir in new[] { root, docs, batchDir })
        {
            var full = Path.GetFullPath(Path.Combine(baseDir, m.Value.TrimStart('/', '\\')));
            if (!File.Exists(full) || !full.StartsWith(docs + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            var rel = Path.GetRelativePath(docs, full).Replace('\\', '/');
            if (!refs.Contains(rel)) refs.Add(rel);
            break;
        }
    var ph = System.Text.RegularExpressions.Regex.Match(name, @"-p(\d+)(?:-|$)");
    if (ph.Success)
        foreach (var f in Directory.EnumerateFiles(batchDir, $"*-phase{ph.Groups[1].Value}.md"))
        {
            var rel = Path.GetRelativePath(docs, f).Replace('\\', '/');
            refs.Remove(rel);
            refs.Insert(0, rel); // Phase doc luôn là tab tài liệu đầu tiên
        }
    return Results.Json(new { brief, docs = refs, touched = Touched(file).Select(kv => new { path = kv.Key, count = kv.Value }).OrderBy(x => x.path) });
});

// Nội dung một tài liệu .md/.txt nằm dưới project-docs (không stream).
app.MapGet("/api/doc", (string path) =>
{
    var full = Path.GetFullPath(Path.Combine(docs, path));
    if (!full.StartsWith(docs + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || full.Replace('\\', '/').Contains("/_workers/streams/")
        || !(full.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || full.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)) || !File.Exists(full))
        return Results.NotFound();
    var text = File.ReadAllText(full);
    return Results.Text(Shorten(text, 400_000), "text/plain; charset=utf-8");
});

// Các dòng cuối workflow log của đợt.
app.MapGet("/api/batchlog", (string batch) =>
{
    var dir = Path.GetFullPath(Path.Combine(docs, batch));
    if (Path.GetDirectoryName(dir) != docs || !Directory.Exists(dir)) return Results.NotFound();
    var log = Directory.EnumerateFiles(dir, "*workflow-log.md").FirstOrDefault();
    if (log is null) return Results.Json(new { path = (string?)null, lines = Array.Empty<string>() });
    var lines = File.ReadAllLines(log).Where(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"^\d{4}-\d{2}-\d{2} ")).TakeLast(40).ToArray();
    return Results.Json(new { path = Path.GetRelativePath(docs, log).Replace('\\', '/'), lines });
});

// Chế độ "Văn phòng" (3D): file tĩnh trong scripts/office/ — office.js + assets/ do fetch-assets.py tải về.
var officeDir = Path.Combine(docs, "automation", "scripts", "office");
app.MapGet("/office/{**path}", (string path, HttpContext ctx) =>
{
    var full = Path.GetFullPath(Path.Combine(officeDir, path));
    if (!full.StartsWith(officeDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(full)) return Results.NotFound();
    var type = Path.GetExtension(full).ToLowerInvariant() switch
    {
        ".js" => "text/javascript; charset=utf-8", ".json" => "application/json; charset=utf-8",
        ".jpg" => "image/jpeg", ".png" => "image/png", ".fbx" => "application/octet-stream", _ => null,
    };
    if (type is null) return Results.NotFound();
    // file nặng trong assets/ cache 1 ngày; manifest.json và code luôn hỏi lại (chạy lại fetch-assets.py là thấy ngay)
    ctx.Response.Headers.CacheControl = full.Contains($"{Path.DirectorySeparatorChar}assets{Path.DirectorySeparatorChar}") && !full.EndsWith("manifest.json") ? "max-age=86400" : "no-cache";
    return Results.Stream(File.OpenRead(full), type);
});

// ---------- Thông báo Telegram: chạy nền trong viewer, không cần mở trình duyệt ----------
// Cấu hình: scripts/telegram.local.json {"botToken","chatId"} (gitignore) hoặc biến môi trường WB_TG_TOKEN / WB_TG_CHAT.
// Gửi: lượt bắt đầu / xong / lỗi, kết luận SA gate + Reviewer, lượt có vẻ treo, Leader đang chờ User trả lời. Trong group gõ /status ⇒ bot trả tình hình.
var tg = LoadTelegram(Path.Combine(docs, "automation", "scripts", "telegram.local.json"));
var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
var mutedUntil = DateTime.MinValue;                       // /mute 2h ⇒ không gửi thông báo tự động tới lúc này (lệnh vẫn trả lời)
string[][] BotCommands = [
    ["status", "Tình hình: đợt, Phase, lượt đang chạy"], ["phase", "Phase đang làm gì: mục tiêu, checkpoint xong/đang/chưa"],
    ["cp", "Checkpoint đang làm: nội dung + worker đang nói gì"], ["next", "Kế hoạch tiếp theo: Leader định làm gì, Phase kế"],
    ["why", "Vì sao lâu: lượt đang chạy làm gì, chờ lệnh nào bao lâu"],
    ["leader", "Leader đang làm gì / đang chờ gì"], ["log", "Dòng cuối workflow log (vd /log 20)"], ["cost", "Chi phí hôm nay và của đợt"],
    ["mute", "Tạm tắt thông báo (vd /mute 2h, /mute 30m)"], ["unmute", "Bật lại thông báo"], ["help", "Danh sách lệnh"]];
if (tg is { } cfg)
{
    _ = Task.Run(NotifyLoop); _ = Task.Run(CommandLoop); _ = Task.Run(RegisterCommands);
    _ = Task.Run(() => SendTelegram("🟢 <b>Worker board</b> đã bật thông báo. Gõ /status để xem tình hình."));
    Console.WriteLine($"Telegram: bật — gửi vào chat {cfg.Chat}");
}

async Task SendTelegram(string html)
{
    if (tg is not { } c) return;
    var res = await http.PostAsync($"https://api.telegram.org/bot{c.Token}/sendMessage", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["chat_id"] = c.Chat, ["text"] = html.Length > 4000 ? html[..4000] + "…" : html, ["parse_mode"] = "HTML", ["disable_web_page_preview"] = "true",
    }));
    Console.WriteLine(res.IsSuccessStatusCode ? $"{DateTime.Now:HH:mm:ss} Telegram → {html.Split('\n')[0]}" : $"Telegram: gửi lỗi {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
}

async Task NotifyLoop()
{
    Dictionary<string, StreamInfo>? prev = null;
    var staleSent = new HashSet<string>(); var waitSent = new Dictionary<string, string>();
    // Gập laptop ⇒ máy ngủ, mọi tiến trình (viewer, Leader, worker) đứng. Thức dậy: hai vòng cách nhau > 2 phút.
    // Thời gian im lặng chỉ tính từ lúc máy thức (awakeSince) ⇒ không báo "treo"/"Leader chờ" giả vì quãng ngủ.
    var lastTick = DateTime.UtcNow; var awakeSince = DateTime.UtcNow;
    while (true)
    {
        try
        {
            var gap = DateTime.UtcNow - lastTick; lastTick = DateTime.UtcNow;
            var lines = new List<string>();
            if (gap > TimeSpan.FromMinutes(2))
            {
                awakeSince = DateTime.UtcNow;
                lines.Add($"💤 Máy vừa ngủ ~{(int)gap.TotalMinutes} phút (gập laptop?) — Leader và worker đã tạm dừng trong lúc đó, giờ chạy tiếp.");
            }
            TimeSpan Quiet(DateTime modified) => DateTime.UtcNow - (modified > awakeSince ? modified : awakeSince);
            var now = AllStreams(); var first = prev is null;
            var workers = now.Where(i => i.role != "Leader").ToList();
            foreach (var i in workers)
            {
                StreamInfo? p = null; prev?.TryGetValue(i.id, out p);
                if (!first && p is null && i.running) lines.Add($"▶️ Bắt đầu: <b>{H(Label(i))}</b> · {H(i.batch)}");   // lượt mới = file stream mới
                if (!first && p is { ended: false } && i.ended) lines.Add(FinishLine(i));
                if (i.ended || i.running) staleSent.Remove(i.id);      // chạy lại / xong ⇒ lần treo sau báo tiếp
                else if (DateTime.UtcNow - i.modified < TimeSpan.FromHours(2) && Quiet(i.modified) > TimeSpan.FromMinutes(10) && staleSent.Add(i.id) && !first)
                    lines.Add($"⏳ Có vẻ treo: <b>{H(Label(i))}</b> — {(int)Quiet(i.modified).TotalMinutes} phút không có sự kiện · {H(i.batch)}");
            }
            // Leader đang chờ User: phiên Leader im 3–60 phút (không tính lúc máy ngủ), lượt cuối là câu trả lời (không phải lệnh đang chờ),
            // và không worker nào của đợt phiên đó điều phối đang chạy (đang chờ worker thì không phải chờ User).
            foreach (var l in now.Where(i => i.role == "Leader" && Quiet(i.modified) > TimeSpan.FromMinutes(3) && DateTime.UtcNow - i.modified < TimeSpan.FromHours(1)))
                if (Resolve(l.id) is { } f && !WaitsOnWorker(f, workers) && LeaderWaiting(f) is { } w && (!waitSent.TryGetValue(l.id, out var k) || k != w.Key))
                    {
                        waitSent[l.id] = w.Key;
                        if (!first) lines.Add($"🙋 <b>Leader đang chờ bạn</b> ({H(l.name)}):\n<i>{H(w.Text)}</i>");
                    }
            if (lines.Count > 0 && DateTime.UtcNow >= mutedUntil) await SendTelegram(string.Join("\n", lines));
            prev = now.ToDictionary(i => i.id);
        }
        catch (Exception e) { Console.WriteLine("Telegram: " + e.Message); }
        await Task.Delay(15000);
    }
}

// /status trong group (chỉ đúng chat đã cấu hình) ⇒ trả tình hình hiện tại. Lần đầu bỏ qua tin cũ còn tồn.
async Task CommandLoop()
{
    long offset = -1; var skipBacklog = true;
    while (true)
    {
        try
        {
            using var doc = JsonDocument.Parse(await http.GetStringAsync($"https://api.telegram.org/bot{tg!.Value.Token}/getUpdates?timeout=25&offset={offset}"));
            foreach (var u in doc.RootElement.GetProperty("result").EnumerateArray())
            {
                offset = u.GetProperty("update_id").GetInt64() + 1;
                if (skipBacklog || !u.TryGetProperty("message", out var m)) continue;
                var chat = m.GetProperty("chat").GetProperty("id").GetRawText();
                var text = Str(m, "text").Trim();
                if (chat != tg!.Value.Chat || !text.StartsWith('/')) continue;
                var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts[0].Split('@')[0].ToLowerInvariant(); var arg = parts.Length > 1 ? parts[1].Trim() : "";
                string? reply = cmd switch
                {
                    "/status" => StatusText(), "/why" => WhyText(), "/phase" => PhaseText(arg), "/cp" => CpText(), "/next" => NextText(), "/leader" => LeaderText(), "/log" => LogText(arg),
                    "/cost" => CostText(), "/mute" => Mute(arg), "/unmute" => Mute("0"), "/help" or "/start" => HelpText(), _ => null,
                };
                if (reply is not null) await SendTelegram(reply);
            }
            if (skipBacklog) { skipBacklog = false; if (offset < 0) offset = 0; }
        }
        catch (Exception e) { Console.WriteLine("Telegram getUpdates: " + e.Message); await Task.Delay(10000); }
    }
}

// Xem trước câu trả lời của bot, không gửi gì: /api/tg-status, /api/tg?cmd=why|leader|log|cost|help
app.MapGet("/api/tg-status", () => Results.Text(StatusText(), "text/plain; charset=utf-8"));
app.MapGet("/api/tg", (string cmd, string? arg) => Results.Text(cmd switch
{
    "why" => WhyText(), "phase" => PhaseText(arg ?? ""), "cp" => CpText(), "next" => NextText(), "leader" => LeaderText(), "log" => LogText(arg ?? ""), "cost" => CostText(), "help" => HelpText(), _ => StatusText(),
}, "text/plain; charset=utf-8"));

async Task RegisterCommands()                             // menu lệnh khi gõ "/" trong Telegram
{
    var body = JsonSerializer.Serialize(new { commands = BotCommands.Select(c => new { command = c[0], description = c[1] }) });
    try { await http.PostAsync($"https://api.telegram.org/bot{tg!.Value.Token}/setMyCommands", new StringContent(body, Encoding.UTF8, "application/json")); }
    catch (Exception e) { Console.WriteLine("Telegram setMyCommands: " + e.Message); }
}

string HelpText() => "🤖 <b>Lệnh</b>\n" + string.Join("\n", BotCommands.Select(c => $"/{c[0]} — {H(c[1])}"));

string Mute(string arg)
{
    var m = System.Text.RegularExpressions.Regex.Match(arg, @"^(\d+)\s*([hm]?)$");
    var span = arg == "0" ? TimeSpan.Zero : m.Success ? (m.Groups[2].Value == "m" ? TimeSpan.FromMinutes(int.Parse(m.Groups[1].Value)) : TimeSpan.FromHours(int.Parse(m.Groups[1].Value))) : TimeSpan.FromHours(1);
    mutedUntil = DateTime.UtcNow + span;
    return span == TimeSpan.Zero ? "🔔 Đã bật lại thông báo." : $"🔕 Tạm tắt thông báo tới {mutedUntil.ToLocalTime():HH:mm} ({Dur(span)}). /unmute để bật lại sớm.";
}

// Vì sao lâu: mỗi lượt đang chạy — thời gian, số lệnh, lệnh đang chờ kết quả bao lâu, phân bố loại việc, so với các lượt cùng vai trước đó.
string WhyText()
{
    var all = AllStreams(); var workers = all.Where(i => i.role != "Leader").ToList();
    var running = workers.Where(i => i.running).ToList();
    var sb = new StringBuilder();
    if (running.Count == 0)
    {
        sb.AppendLine("Không có lượt worker nào đang chạy.");
        var stale = workers.Where(i => !i.running && !i.ended && DateTime.UtcNow - i.modified < TimeSpan.FromHours(2)).ToList();
        foreach (var i in stale) sb.AppendLine($"⏳ {H(Label(i))} im {Dur(DateTime.UtcNow - i.modified)} mà chưa có kết quả — có thể treo hoặc bị dừng.");
        sb.Append(LeaderText());
        return sb.ToString();
    }
    foreach (var i in running)
    {
        var st = Resolve(i.id) is { } f ? StreamStats(f) : default;
        var quiet = DateTime.UtcNow - i.modified;
        sb.AppendLine($"⏱ <b>{H(Label(i))}</b> · {H(i.batch)}\n   Chạy {Dur(DateTime.UtcNow - i.created)} · {st.Tools} lệnh");
        if (st.Pending is { } p) sb.AppendLine($"   Đang chờ kết quả {Dur(quiet)}: <code>{H(Cut(p, 110))}</code>");
        else if (i.last is { } l) sb.AppendLine($"   Việc gần nhất ({Dur(quiet)} trước): <code>{H(Cut(l, 110))}</code>");
        if (st.ByTool?.Count > 0) sb.AppendLine("   Gồm: " + string.Join(" · ", st.ByTool.OrderByDescending(kv => kv.Value).Take(5).Select(kv => $"{kv.Key} {kv.Value}")));
        var past = workers.Where(x => x.role == i.role && x.ended && x.id != i.id).OrderByDescending(x => x.modified).Take(4)
            .Select(x => Dur(x.modified - x.created)).ToList();
        if (past.Count > 0) sb.AppendLine($"   Các lượt {H(i.role)} trước: {string.Join(", ", past)}");
        // nhận xét ngắn: thứ hay làm lượt dài
        var hint = st.Pending is { } pc && System.Text.RegularExpressions.Regex.IsMatch(pc, @"dotnet test|\btest\b|e2e|playwright|selenium", System.Text.RegularExpressions.RegexOptions.IgnoreCase) && quiet > TimeSpan.FromMinutes(3) ? "đang chạy bộ test — thường là phần lâu nhất"
            : st.Pending is { } pb && System.Text.RegularExpressions.Regex.IsMatch(pb, @"docker|compose|build|restore", System.Text.RegularExpressions.RegexOptions.IgnoreCase) && quiet > TimeSpan.FromMinutes(3) ? "đang build / dựng stack Docker"
            : quiet > TimeSpan.FromMinutes(8) ? "im khá lâu — có thể đang chờ một lệnh dài, nếu quá 10 phút viewer sẽ báo treo"
            : st.Tools > 0 && st.ByTool!.GetValueOrDefault("Bash") + st.ByTool!.GetValueOrDefault("PowerShell") > st.Tools / 2 ? "chủ yếu chạy lệnh (build/test/smoke)"
            : st.Tools > 0 && st.ByTool!.GetValueOrDefault("Read") + st.ByTool!.GetValueOrDefault("Grep") + st.ByTool!.GetValueOrDefault("Glob") > st.Tools / 2 ? "chủ yếu đọc code/tài liệu" : null;
        if (hint is not null) sb.AppendLine($"   💡 {hint}");
    }
    return sb.ToString();
}

// Đếm lệnh theo loại + lệnh cuối chưa có kết quả (đang chạy). Đọc cả file stream — chỉ gọi khi có lệnh /why.
static (int Tools, Dictionary<string, int>? ByTool, string? Pending) StreamStats(string file)
{
    var by = new Dictionary<string, int>(); var open = new Dictionary<string, string>(); int n = 0;
    using var fs = OpenShared(file); using var sr = new StreamReader(fs, Encoding.UTF8);
    while (sr.ReadLine() is { } line)
    {
        if (!line.Contains("\"tool_")) continue;
        if (TryParse(line) is not { } j) continue;
        foreach (var c in Content(j))
        {
            var ct = Str(c, "type");
            if (ct == "tool_use") { n++; var name = Str(c, "name"); by[name] = by.GetValueOrDefault(name) + 1; open[Str(c, "id")] = name + ": " + (c.TryGetProperty("input", out var inp) ? InputText(inp) : ""); }
            else if (ct == "tool_result") open.Remove(Str(c, "tool_use_id"));
        }
    }
    return (n, by, open.Count > 0 ? open.Values.Last() : null);
}

static string Dur(TimeSpan t) => t.TotalMinutes < 1 ? "dưới 1 phút" : t.TotalHours < 1 ? $"{(int)t.TotalMinutes} phút" : $"{(int)t.TotalHours} giờ {t.Minutes} phút";

// Leader: mỗi phiên hoạt động trong 2 giờ — đang làm gì / chờ worker / chờ bạn, kèm câu nói gần nhất.
string LeaderText()
{
    var all = AllStreams(); var workers = all.Where(i => i.role != "Leader").ToList();
    var sb = new StringBuilder();
    foreach (var l in all.Where(i => i.role == "Leader" && DateTime.UtcNow - i.modified < TimeSpan.FromHours(2)))
    {
        if (Resolve(l.id) is not { } f) continue;
        var state = l.running ? "đang làm" : WaitsOnWorker(f, workers) ? "đang chờ worker" : LeaderWaiting(f) is not null ? "🙋 đang chờ bạn" : $"im {Dur(DateTime.UtcNow - l.modified)}";
        sb.AppendLine($"🧭 <b>{H(l.name)}</b> — {state}");
        if (l.running && l.last is { } a) sb.AppendLine($"   <code>{H(Cut(a, 110))}</code>");
        if (LastLeaderText(f) is { } t) sb.AppendLine($"   <i>{H(Cut(t, 500))}</i>");
    }
    return sb.Length == 0 ? "Không có phiên Leader nào hoạt động trong 2 giờ qua." : sb.ToString();
}
static string? LastLeaderText(string file)
{
    foreach (var line in Tail(file, 512 * 1024).Split('\n').Reverse())
        if (TryParse(line) is { } j && Str(j, "type") == "assistant")
        {
            var t = string.Join("\n", Content(j).Where(b => Str(b, "type") == "text").Select(b => Str(b, "text"))).Trim();
            if (t.Length > 0) return t.Length > 500 ? "…" + t[^500..] : t;
        }
    return null;
}

// Workflow log của đợt có hoạt động gần nhất: n dòng cuối (mặc định 10), mới nhất ở dưới.
string LogText(string arg)
{
    var n = int.TryParse(arg, out var k) ? Math.Clamp(k, 1, 40) : 10;
    var batch = AllStreams().Where(i => i.role != "Leader").OrderByDescending(i => i.modified).FirstOrDefault()?.batch;
    var log = batch is null ? null : Directory.EnumerateFiles(Path.Combine(docs, batch), "*workflow-log.md").FirstOrDefault();
    if (log is null) return "Chưa có workflow log.";
    var lines = File.ReadAllLines(log).Where(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"^\d{4}-\d{2}-\d{2} ")).TakeLast(n)
        .Select(l => "• " + H(Cut(System.Text.RegularExpressions.Regex.Replace(l, @"^\d{4}-\d{2}-\d{2} (\d{2}:\d{2}) — (\w+) — [^—]+ — ", "$1 $2: "), 280)));
    return $"📜 <b>{H(batch!)}</b> — {n} dòng cuối workflow log\n" + string.Join("\n", lines);
}

// ---- Phase / checkpoint / kế hoạch: đọc tài liệu đợt (Epic "## Phases", Phase doc "## Kế hoạch" CP0…CPn) + lời worker/Leader gần nhất

PhaseInfo? GetPhase(string arg)
{
    var workers = AllStreams().Where(i => i.role != "Leader").ToList();
    var latest = workers.OrderByDescending(i => i.modified).FirstOrDefault(); if (latest is null) return null;
    var batch = latest.batch; var bw = workers.Where(i => i.batch == batch).ToList();
    var pm = System.Text.RegularExpressions.Regex.Match(arg, @"(\d+)");
    var ph = pm.Success ? "P" + pm.Groups[1].Value : PhaseOf(bw.Where(i => i.running).OrderByDescending(i => i.modified).FirstOrDefault() ?? latest);
    var dir = Path.Combine(docs, batch);
    var doc = ph.StartsWith('P') ? Directory.EnumerateFiles(dir, $"*-phase{ph[1..]}.md").FirstOrDefault() : null;
    var text = doc is null ? "" : File.ReadAllText(doc);
    var title = doc is null ? null : CleanMd(text.Split('\n')[0].TrimStart('#', ' '));
    var inPhase = bw.Where(i => PhaseOf(i) == ph).OrderBy(i => i.created).ToList();
    // Dev lượt 1 (-r1) = đọc doc + phản hồi (+ spike CP0): nhắc CP khác là đang bàn kế hoạch ⇒ chỉ tính CP0. Lượt code (r2+, fix) ⇒ tính mọi CP.
    string? cp = null; var cpFromCode = false;
    foreach (var d in inPhase.Where(i => i.role == "Dev"))
        if (Resolve(d.id) is { } f && LastCpMention(f, ph.TrimStart('P'), d.name.EndsWith("-r1")) is { } c) { cp = c; cpFromCode = !d.name.EndsWith("-r1"); }
    var run = inPhase.LastOrDefault(i => i.running);
    var says = run is not null && Resolve(run.id) is { } rf ? LastLeaderText(rf) : null;   // câu nói cuối của worker (cùng dạng assistant text)
    return new PhaseInfo(batch, ph, title, PlanItems(text), cp, cpFromCode, run, says, EpicPhases(dir), inPhase);
}

string PhaseText(string arg)
{
    if (GetPhase(arg) is not { } p) return "Chưa có lượt nào.";
    var sb = new StringBuilder($"📌 <b>{H(p.Batch)}</b>\n");
    foreach (var e in p.Epic) sb.AppendLine("   " + H(Cut(e, 110)));
    sb.AppendLine($"\n🎯 <b>{H(p.Title is { } t ? System.Text.RegularExpressions.Regex.Replace(t, @"^.*?Phase\s+", "") : p.Phase)}</b>");
    var marks = CpMarks(p);
    if (p.Plan.Count == 0) sb.AppendLine("   (Phase doc chưa có mục Kế hoạch theo checkpoint)");
    for (var k = 0; k < p.Plan.Count; k++) sb.AppendLine($"{marks[k]} <b>{H(p.Plan[k].Cp)}</b> — {H(Cut(p.Plan[k].Text, 120))}");
    if (p.Run is { } r) sb.AppendLine($"\n▶️ Đang chạy: <b>{H(Label(r))}</b> · {Dur(DateTime.UtcNow - r.created)}" + (p.Says is { } s ? $"\n   <i>{H(Cut(LastParagraph(s), 300))}</i>" : ""));
    else sb.AppendLine($"\nKhông có lượt nào của {H(p.Phase)} đang chạy." + (p.InPhase.LastOrDefault() is { } l ? $" Lượt cuối: {H(Label(l))}{(l.verdict is { } v ? " — " + H(v) : "")}." : ""));
    return sb.ToString();
}

string CpText()
{
    if (GetPhase("") is not { } p) return "Chưa có lượt nào.";
    var (idx, planned) = CurrentCp(p);
    var sb = new StringBuilder($"🎯 <b>{H(p.Phase)}</b> · {H(p.Batch)}\n");
    var lastDev = p.InPhase.LastOrDefault(i => i.role == "Dev");
    if (planned) sb.AppendLine("<i>Dev vừa vào lượt code, chưa báo checkpoint — theo kế hoạch đang làm:</i>");
    if (lastDev is not null && lastDev.name.EndsWith("-r1"))   // chưa vào lượt code
        sb.AppendLine(lastDev.running ? "📖 Dev đang ở <b>lượt 1</b>: đọc Phase doc, phản hồi, spike CP0 — chưa vào code."
            : "📖 Dev đã xong <b>lượt 1</b> (phản hồi doc + spike CP0) — chờ Leader duyệt đồng thuận rồi giao lượt code.");
    if (idx < 0) sb.AppendLine(p.Cp is null ? "Chưa thấy Dev nhắc tới checkpoint nào (có thể đang ở lượt đọc doc / review / sửa sau review)." : $"Dev đang nhắc {H(p.Cp)} nhưng Phase doc không có mục này.");
    else
    {
        sb.AppendLine($"▶️ <b>{H(p.Plan[idx].Cp)}</b> ({idx + 1}/{p.Plan.Count}): {H(Cut(p.Plan[idx].Text, 700))}");
        if (idx + 1 < p.Plan.Count) sb.AppendLine($"⏭ Sau đó: <b>{H(p.Plan[idx + 1].Cp)}</b> — {H(Cut(p.Plan[idx + 1].Text, 200))}");
    }
    if (p.Run is { } r)
        sb.AppendLine(p.Says is { } s ? $"\n💬 <b>{H(Label(r))}</b> ({Dur(DateTime.UtcNow - r.created)}): <i>{H(Cut(LastParagraph(s), 400))}</i>"
            : $"\n⚙️ <b>{H(Label(r))}</b> ({Dur(DateTime.UtcNow - r.created)}) đang: <code>{H(Cut(r.last ?? "…", 160))}</code>");
    return sb.ToString();
}

string NextText()
{
    if (GetPhase("") is not { } p) return "Chưa có lượt nào.";
    var all = AllStreams(); var sb = new StringBuilder($"🗺 <b>{H(p.Batch)}</b>\n");
    // Leader điều phối đợt này: phiên có nhắc tên đợt gần đây, mới nhất
    var leader = all.Where(i => i.role == "Leader" && DateTime.UtcNow - i.modified < TimeSpan.FromHours(6))
        .Where(i => Resolve(i.id) is { } f && Tail(f, 512 * 1024).Contains(p.Batch)).OrderByDescending(i => i.modified).FirstOrDefault();
    if (leader is not null && Resolve(leader.id) is { } lf && LastLeaderText(lf) is { } lt)
        sb.AppendLine($"🧭 Leader nói gần nhất ({Dur(DateTime.UtcNow - leader.modified)} trước):\n<i>{H(Cut(LastParagraph(lt, 2), 700))}</i>");
    var todo = p.Epic.Where(e => !e.StartsWith("✅")).ToList();
    sb.AppendLine(todo.Count == 0 ? "\n✅ Mọi Phase trong Epic đã xong — tiếp theo là bàn giao / đóng đợt." : "\n⏭ Phase còn lại:\n" + string.Join("\n", todo.Select(e => "   " + H(Cut(e, 140)))));
    var log = Directory.EnumerateFiles(Path.Combine(docs, p.Batch), "*workflow-log.md").FirstOrDefault();
    var lastLog = log is null ? null : File.ReadAllLines(log).LastOrDefault(l => System.Text.RegularExpressions.Regex.IsMatch(l, @"^\d{4}-\d{2}-\d{2} "));
    if (lastLog is not null) sb.AppendLine($"\n📜 Việc ghi log gần nhất: {H(Cut(System.Text.RegularExpressions.Regex.Replace(lastLog, @"^\d{4}-\d{2}-\d{2} (\d{2}:\d{2}) — (\w+) — [^—]+ — ", "$1 $2: "), 300))}");
    return sb.ToString();
}

// ✅ xong · ▶️ đang làm · ○ chưa: Reviewer PASS (hoặc có Reviewer sau Dev cuối) ⇒ tất cả xong; còn lại theo CP Dev nhắc gần nhất.
static string[] CpMarks(PhaseInfo p)
{
    var marks = Enumerable.Repeat("○", p.Plan.Count).ToArray();
    var lastDev = p.InPhase.LastOrDefault(i => i.role == "Dev");
    var allDone = p.InPhase.Any(i => i.role == "Reviewer" && i.verdict == "PASS")
        || (lastDev is { running: false } && p.InPhase.Any(i => i.role == "Reviewer" && i.created > lastDev.created));
    var (idx, planned) = CurrentCp(p);
    for (var k = 0; k < marks.Length; k++)
        marks[k] = allDone || k < idx ? "✅" : k == idx ? (lastDev is { running: true } ? "▶️" : "✅") : "○";
    if (planned && idx >= 0 && !allDone) marks[idx] = "▶️";
    return marks;
}

// Checkpoint đang làm: CP Dev nhắc gần nhất; lượt code đang chạy mà chưa nhắc CP nào ⇒ CP kế tiếp theo kế hoạch (planned = true).
static (int Index, bool Planned) CurrentCp(PhaseInfo p)
{
    var idx = p.Plan.FindIndex(x => x.Cp == p.Cp);
    var lastDev = p.InPhase.LastOrDefault(i => i.role == "Dev");
    if (lastDev is { running: true } && !lastDev.name.EndsWith("-r1") && !p.CpFromCode && idx + 1 < p.Plan.Count) return (idx + 1, true);
    return (idx, false);
}

// Mục "## Kế hoạch" của Phase doc: dòng "1. **CP0 …**: …" ⇒ (CP0, nội dung)
static List<(string Cp, string Text)> PlanItems(string phaseDoc)
{
    var items = new List<(string, string)>(); var inPlan = false;
    foreach (var raw in phaseDoc.Split('\n'))
    {
        var line = raw.TrimEnd('\r');
        if (line.StartsWith("## ")) { if (inPlan) break; inPlan = line.Contains("Kế hoạch"); continue; }
        if (!inPlan) continue;
        var m = System.Text.RegularExpressions.Regex.Match(line, @"^\s*(\d+)\.\s+(.+)$");
        if (!m.Success) continue;
        var text = CleanMd(m.Groups[2].Value);
        var cp = System.Text.RegularExpressions.Regex.Match(text, @"\bCP\d+[a-z]?\b");
        items.Add((cp.Success ? cp.Value : "Bước " + m.Groups[1].Value,
                   cp.Success ? System.Text.RegularExpressions.Regex.Replace(text, @"^\s*" + cp.Value + @"[\s:]*", "").Trim() : text));
    }
    return items;
}

// Epic "## Phases": mỗi dòng "- ✅ **[Phase P1: …](…)** — …"
static List<string> EpicPhases(string batchDir)
{
    var list = new List<string>();
    foreach (var f in Directory.EnumerateFiles(batchDir, "*-epic*.md").Where(f => !f.Contains("-phase")).Order())
    {
        var inSec = false;
        foreach (var raw in File.ReadAllLines(f))
        {
            if (raw.StartsWith("## ")) { if (inSec) break; inSec = raw.StartsWith("## Phases"); continue; }
            if (inSec && raw.TrimStart().StartsWith("- ")) list.Add(CleanMd(raw.TrimStart()[2..]));
        }
    }
    return list;
}

// CP Dev nhắc gần nhất: lời nói (text) của Dev, hoặc dấu "DONE dev-p<N>-cp<X>" đúng số Phase (dấu của Phase khác là do Dev đọc file cũ).
// onlyCp0: lượt phản hồi doc ⇒ chỉ nhận CP0 (spike), bỏ các lần nhắc CP khác khi bàn kế hoạch.
static string? LastCpMention(string streamFile, string phaseNum, bool onlyCp0)
{
    string? cp = null;
    var doneRx = new System.Text.RegularExpressions.Regex($@"DONE dev-p{phaseNum}-cp(\d+[a-z]?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    using var fs = OpenShared(streamFile); using var sr = new StreamReader(fs, Encoding.UTF8);
    while (sr.ReadLine() is { } line)
    {
        if (!line.Contains("\"assistant\"") || !line.Contains("CP", StringComparison.OrdinalIgnoreCase) || TryParse(line) is not { } j) continue;
        foreach (var c in Content(j))
        {
            var isText = Str(c, "type") == "text";
            var src = isText ? Str(c, "text") : Str(c, "type") == "tool_use" && c.TryGetProperty("input", out var inp) ? inp.GetRawText() : "";
            var found = new List<string>();
            foreach (System.Text.RegularExpressions.Match m in doneRx.Matches(src)) found.Add("CP" + m.Groups[1].Value.ToUpperInvariant());
            if (isText) foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(src, @"\bCP(\d+[a-z]?)\b")) found.Add("CP" + m.Groups[1].Value);
            if (onlyCp0 && isText && src.Contains("spike", StringComparison.OrdinalIgnoreCase)) found.Add("CP0");   // lượt 1 gọi CP0 là "spike Sx"
            foreach (var f in found) if (!onlyCp0 || f == "CP0") cp = f;
        }
    }
    return cp;
}

static string CleanMd(string s)
{
    s = System.Text.RegularExpressions.Regex.Replace(s, @"\[([^\]]+)\]\([^)]*\)", "$1");
    s = s.Replace("**", "").Replace("🔴 ", "");
    return System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
}
static string LastParagraph(string s, int n = 1)
{
    var paras = s.Split("\n\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
    return paras.Count == 0 ? s : string.Join("\n\n", paras.TakeLast(n));
}

string CostText()
{
    var workers = AllStreams().Where(i => i.role != "Leader" && i.cost is not null).ToList();
    var today = workers.Where(i => i.modified.ToLocalTime().Date == DateTime.Now.Date).ToList();
    var batch = workers.OrderByDescending(i => i.modified).FirstOrDefault()?.batch;
    var inBatch = workers.Where(i => i.batch == batch).ToList();
    string ByRole(List<StreamInfo> l) => string.Join(" · ", l.GroupBy(i => i.role).OrderByDescending(g => g.Sum(i => i.cost)).Select(g => $"{H(g.Key)} ${g.Sum(i => i.cost):0.00}"));
    return $"💰 <b>Hôm nay</b>: ${today.Sum(i => i.cost):0.00} ({today.Count} lượt)\n   {ByRole(today)}\n" +
           $"💰 <b>{H(batch ?? "—")}</b>: ${inBatch.Sum(i => i.cost):0.00} ({inBatch.Count} lượt)\n   {ByRole(inBatch)}\n" +
           "<i>Chỉ tính worker (stream ghi chi phí); phiên Leader không có số chi phí.</i>";
}

string StatusText()
{
    var all = AllStreams(); var workers = all.Where(i => i.role != "Leader").ToList();
    var batch = workers.OrderByDescending(i => i.modified).FirstOrDefault()?.batch ?? "—";   // đợt có hoạt động gần nhất
    var sb = new StringBuilder($"📋 <b>{H(batch)}</b>\n");
    var phases = workers.Where(i => i.batch == batch).GroupBy(PhaseOf).OrderBy(g => g.Key == "Discovery" ? -1 : int.TryParse(g.Key.TrimStart('P'), out var n) ? n : 900);
    sb.AppendLine(string.Join(" · ", phases.Select(g =>
    {
        var run = g.Any(i => i.running); var v = g.OrderBy(i => i.created).LastOrDefault(i => i.verdict is not null)?.verdict;
        return $"{H(g.Key)} {(run ? "⟳" : v == "PASS" ? "✅" : v is not null ? "❌" : "·")}";
    })));
    var running = workers.Where(i => i.running).ToList();
    if (running.Count == 0) sb.AppendLine("Không có lượt worker nào đang chạy.");
    foreach (var i in running)
        sb.AppendLine($"▶️ <b>{H(Label(i))}</b> · {(int)(DateTime.UtcNow - i.created).TotalMinutes} phút{(i.batch != batch ? " · " + H(i.batch) : "")}\n   └ <code>{H(Cut(i.last ?? "…", 120))}</code>");
    foreach (var i in workers.Where(i => !i.running && !i.ended && DateTime.UtcNow - i.modified < TimeSpan.FromHours(2)))
        sb.AppendLine($"⏳ Có vẻ treo: {H(Label(i))} — {(int)(DateTime.UtcNow - i.modified).TotalMinutes} phút");
    var leader = all.Where(i => i.role == "Leader").OrderByDescending(i => i.modified).FirstOrDefault();
    if (leader is not null)
        sb.AppendLine(leader.running ? $"🧭 Leader đang làm: <code>{H(Cut(leader.last ?? "…", 120))}</code>"
            : Resolve(leader.id) is { } f && !WaitsOnWorker(f, workers) && LeaderWaiting(f) is { } w ? $"🙋 Leader đang chờ bạn:\n<i>{H(w.Text)}</i>"
            : $"🧭 Leader im {(int)(DateTime.UtcNow - leader.modified).TotalMinutes} phút");
    return sb.ToString();
}

Console.WriteLine($"Worker viewer: http://127.0.0.1:{port}  (root: {root}) — Ctrl+C để dừng");
app.Run();

// ---------- helpers ----------

static (string Token, string Chat)? LoadTelegram(string file)
{
    string? token = Environment.GetEnvironmentVariable("WB_TG_TOKEN"), chat = Environment.GetEnvironmentVariable("WB_TG_CHAT");
    if ((token is null || chat is null) && File.Exists(file))
    {
        using var d = JsonDocument.Parse(File.ReadAllText(file));
        token ??= Str(d.RootElement, "botToken"); chat ??= Str(d.RootElement, "chatId");
    }
    return string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chat) ? null : (token.Trim(), chat.Trim());
}

static string H(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
static string Cut(string s, int n) => s.Length <= n ? s : s[..n] + "…";

// Cùng quy ước tên với giao diện: sa-gate-p2-r2 ⇒ "SA gate P2 · vòng 2"
static string Label(StreamInfo i)
{
    if (i.role == "Leader") return "Phiên Leader " + i.name[7..];
    var n = System.Text.RegularExpressions.Regex.Replace(i.name, "^sa-", "SA ");
    n = System.Text.RegularExpressions.Regex.Replace(n, "^dev-", "Dev ");
    n = System.Text.RegularExpressions.Regex.Replace(n, "^reviewer-", "Review ");
    n = System.Text.RegularExpressions.Regex.Replace(n, @"-p(\d+)", " P$1");
    n = System.Text.RegularExpressions.Regex.Replace(n, @"-r(\d+)$", " · vòng $1");
    n = System.Text.RegularExpressions.Regex.Replace(n, @"-fix(\d+)", " · sửa $1");
    return n.Replace('-', ' ').Replace(" disc", " discovery");
}
static string PhaseOf(StreamInfo i)
{
    var m = System.Text.RegularExpressions.Regex.Match(i.name, @"-p(\d+)(?:-|$)");
    return m.Success ? "P" + m.Groups[1].Value : System.Text.RegularExpressions.Regex.IsMatch(i.name, "disc|mockup") ? "Discovery" : "Khác";
}
static string FinishLine(StreamInfo i)
{
    var tail = $"{(i.cost is { } c ? $" · ${c:0.00}" : "")}{(i.turns is { } t ? $" · {t} lượt" : "")} · {H(i.batch)}";
    if (i.verdict is { } v) return $"{(v == "PASS" ? "✅" : "❌")} <b>{H(Label(i))}</b>: <b>{H(v)}</b>{tail}";
    return i.isError == true ? $"⚠️ Lỗi: <b>{H(Label(i))}</b>{tail}" : $"🏁 Xong: <b>{H(Label(i))}</b>{tail}";
}

// Phiên Leader đang chờ worker của chính nó: có lượt đang chạy thuộc một đợt mà transcript gần đây nhắc tới (project-docs/<đợt>/).
static bool WaitsOnWorker(string leaderFile, List<StreamInfo> workers)
{
    var running = workers.Where(i => i.running).Select(i => i.batch).ToHashSet();
    if (running.Count == 0) return false;
    var tail = Tail(leaderFile, 512 * 1024);
    return running.Any(b => tail.Contains(b, StringComparison.Ordinal));
}

// Leader đang chờ User: dòng có nghĩa cuối của transcript là câu trả lời của Leader (không phải lệnh đang chờ kết quả).
// Trả về khoá (timestamp — để chỉ báo một lần mỗi lượt) + trích đoạn câu đó.
static (string Key, string Text)? LeaderWaiting(string file)
{
    foreach (var line in Tail(file, 512 * 1024).Split('\n').Reverse())
    {
        if (TryParse(line) is not { } j) continue;
        var type = Str(j, "type");
        if (type == "user") return null;                  // tin của User hoặc kết quả lệnh ⇒ Leader chưa kết thúc lượt
        if (type != "assistant") continue;
        var blocks = Content(j).ToList();
        if (blocks.Any(b => Str(b, "type") == "tool_use")) return null;
        var text = string.Join("\n", blocks.Where(b => Str(b, "type") == "text").Select(b => Str(b, "text"))).Trim();
        if (text.Length == 0) continue;
        return (Str(j, "timestamp"), text.Length > 700 ? "…" + text[^700..] : text);
    }
    return null;
}

static string? FindRepoRoot(string start)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        if (Directory.Exists(Path.Combine(dir.FullName, "project-docs"))) return dir.FullName;
    return null;
}

bool IsTranscript(string file) => file.StartsWith(transcripts + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

string ToId(string file)
{
    var key = IsTranscript(file) ? "t:" + Path.GetFileName(file) : "d:" + Path.GetRelativePath(docs, file).Replace('\\', '/');
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(key)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

// id ⇒ file, chỉ chấp nhận *.jsonl nằm dưới project-docs/*/_workers/streams/ (chặn path traversal).
string? Resolve(string id)
{
    try
    {
        var b64 = id.Replace('-', '+').Replace('_', '/');
        b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
        var key = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        if (key.StartsWith("t:"))
        {
            // Transcript Leader: chỉ tên file .jsonl nằm thẳng trong thư mục transcript (không thư mục con, không '..').
            var fileName = key[2..];
            if (fileName != Path.GetFileName(fileName) || !fileName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)) return null;
            var tx = Path.Combine(transcripts, fileName);
            return File.Exists(tx) ? tx : null;
        }
        if (!key.StartsWith("d:")) return null;
        var rel = key[2..];
        var full = Path.GetFullPath(Path.Combine(docs, rel));
        var norm = full.Replace('\\', '/');
        if (!full.StartsWith(docs + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
        if (!norm.Contains("/_workers/streams/") || !norm.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)) return null;
        return File.Exists(full) ? full : null;
    }
    catch { return null; }
}

static FileStream OpenShared(string file) => new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

// "Đang chạy" = chưa có dòng result và file được ghi trong 10 phút gần đây.
static bool IsRunning(string file) =>
    !HasResult(file) && DateTime.UtcNow - File.GetLastWriteTimeUtc(file) < TimeSpan.FromMinutes(10);

static bool HasResult(string file) => Tail(file).Contains("\"type\":\"result\"");

static string Tail(string file, int bytes = 65536)
{
    using var fs = OpenShared(file);
    var start = Math.Max(0, fs.Length - bytes);
    fs.Seek(start, SeekOrigin.Begin);
    var buf = new byte[fs.Length - start];
    fs.ReadExactly(buf);
    return Encoding.UTF8.GetString(buf);
}

StreamInfo Describe(string file)
{
    var info = new FileInfo(file);
    var rel = Path.GetRelativePath(docs, file).Replace('\\', '/');
    var batch = rel.Split('/')[0];
    var name = Path.GetFileNameWithoutExtension(file).Replace("-stream", "");
    string? session = null, model = null, verdict = null; double? cost = null; int? turns = null; bool? isError = null;
    using (var fs = OpenShared(file))
    using (var sr = new StreamReader(fs, Encoding.UTF8))
    {
        for (var i = 0; i < 5 && sr.ReadLine() is { } line; i++)
        {
            var j = TryParse(line);
            if (j?.GetProperty("type").GetString() == "system" && j.Value.TryGetProperty("session_id", out var sid))
            {
                session = sid.GetString();
                if (j.Value.TryGetProperty("model", out var m)) model = m.GetString();
                break;
            }
        }
    }
    foreach (var line in Tail(file).Split('\n').Reverse())
    {
        var j = TryParse(line);
        if (j is { } r && r.TryGetProperty("type", out var t) && t.GetString() == "result")
        {
            if (r.TryGetProperty("total_cost_usd", out var c) && c.ValueKind == JsonValueKind.Number) cost = c.GetDouble();
            if (r.TryGetProperty("num_turns", out var n) && n.ValueKind == JsonValueKind.Number) turns = n.GetInt32();
            if (r.TryGetProperty("is_error", out var e) && e.ValueKind is JsonValueKind.True or JsonValueKind.False) isError = e.GetBoolean();
            if (name.StartsWith("sa-gate") || name.StartsWith("reviewer")) verdict = VerdictOf(Str(r, "result"));
            break;
        }
    }
    var briefFile = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(file)!)!, "briefs", name + "-brief.txt");
    var brief = File.Exists(briefFile) ? File.ReadLines(briefFile).FirstOrDefault() : null;
    var running = IsRunning(file);
    var role = RoleOf(brief, name);
    // Vai không được sửa code (SA/BA, Reviewer) mà có Edit/Write vào file trong repo nhưng ngoài project-docs ⇒ cảnh báo trên thẻ.
    string? warn = role is "BA/SA" or "Reviewer" && Touched(file).Keys.Any(p => !Path.IsPathRooted(p) && !p.StartsWith("project-docs/"))
        ? "sửa file ngoài project-docs" : null;
    return new StreamInfo(ToId(file), batch, name, role, brief, session, model, cost, turns, isError, info.Length,
        info.CreationTimeUtc, info.LastWriteTimeUtc, running, cost is not null || turns is not null || isError is not null, verdict, warn, null, running ? RecentActions(file) : null);
}

// File lượt worker đã sửa qua tool Edit/Write/MultiEdit/NotebookEdit (lệnh shell ghi file thì không thấy). Đọc tăng dần, nhớ offset theo file.
Dictionary<string, int> Touched(string file)
{
    lock (touchedCache)
    {
        if (!touchedCache.TryGetValue(file, out var c) || c.Offset > new FileInfo(file).Length) c = (0, new Dictionary<string, int>());
        using var fs = OpenShared(file);
        fs.Seek(c.Offset, SeekOrigin.Begin);
        var buf = new byte[fs.Length - c.Offset];
        fs.ReadExactly(buf);
        var lastNl = Array.LastIndexOf(buf, (byte)'\n');
        if (lastNl >= 0)
        {
            foreach (var line in Encoding.UTF8.GetString(buf, 0, lastNl + 1).Split('\n'))
            {
                if (!line.Contains("\"tool_use\"") || TryParse(line) is not { } j || Str(j, "type") != "assistant") continue;
                foreach (var t in Content(j))
                {
                    if (Str(t, "type") != "tool_use" || Str(t, "name") is not ("Edit" or "Write" or "MultiEdit" or "NotebookEdit")
                        || !t.TryGetProperty("input", out var inp)) continue;
                    var p = Str(inp, "file_path") is { Length: > 0 } fp ? fp : Str(inp, "notebook_path");
                    if (p.Length == 0) continue;
                    var full = Path.GetFullPath(Path.IsPathRooted(p) ? p : Path.Combine(root, p));
                    var key = full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        ? Path.GetRelativePath(root, full).Replace('\\', '/') : full;
                    c.Files[key] = c.Files.GetValueOrDefault(key) + 1;
                }
            }
            c.Offset += lastNl + 1;
        }
        touchedCache[file] = c;
        return c.Files;
    }
}

// Kết luận SA gate / Reviewer = chữ in đậm đầu tiên của câu trả lời cuối: "**PASS**", "**BLOCKER (2)**", "**FAIL**", "**chưa PASS — …**".
static string? VerdictOf(string result)
{
    var m = System.Text.RegularExpressions.Regex.Match(result.Length > 600 ? result[..600] : result, @"\*\*(.+?)\*\*");
    if (!m.Success) return null;
    var v = m.Groups[1].Value.Trim();
    var blocker = System.Text.RegularExpressions.Regex.Match(v, @"BLOCKER \(\d+\)");
    if (blocker.Success) return blocker.Value;
    if (v.StartsWith("PASS", StringComparison.OrdinalIgnoreCase)) return "PASS";
    return v.Contains("FAIL", StringComparison.OrdinalIgnoreCase) || v.Contains("chưa PASS", StringComparison.OrdinalIgnoreCase) ? "FAIL" : null;
}

// Các việc gần nhất của lượt đang chạy (lệnh hoặc câu nói), cũ ⇒ mới; việc cuối hiện trên thẻ, cả danh sách hiện trên màn hình laptop ở chế độ Văn phòng.
static List<string>? RecentActions(string file, int n = 8)
{
    var acts = new List<string>();
    foreach (var line in Tail(file, 256 * 1024).Split('\n').Reverse())
    {
        if (TryParse(line) is not { } j || Str(j, "type") != "assistant") continue;
        foreach (var c in Content(j).Reverse())
        {
            var ct = Str(c, "type");
            if (ct == "tool_use") acts.Add(Cut(Str(c, "name") + ": " + (c.TryGetProperty("input", out var inp) ? InputText(inp) : "").Replace('\n', ' '), 140));
            else if (ct == "text" && !string.IsNullOrWhiteSpace(Str(c, "text"))) acts.Add(Cut(Str(c, "text").Trim().Replace('\n', ' '), 140));
            if (acts.Count == n) break;
        }
        if (acts.Count == n) break;
    }
    acts.Reverse();
    return acts.Count > 0 ? acts : null;
}

// Vai: từ tên session ở dòng đầu brief ("Acme-261101-Dev-P2 (resume…)" ⇒ Dev); không có brief ⇒ đoán theo tiền tố tên lượt (dev-, sa-, reviewer-).
static string RoleOf(string? brief, string name)
{
    var token = (brief ?? "").Split(' ', 2)[0];
    var parts = token.Split('-');
    var guess = parts.Length >= 3 ? parts[2] : name.Split('-')[0];
    return guess.ToLowerInvariant() switch
    {
        "dev" => "Dev",
        "sa" or "ba" or "basa" => "BA/SA",
        "reviewer" => "Reviewer",
        "enduser" or "end" => "End User",
        _ => "Khác",
    };
}

StreamInfo DescribeLeader(string file)
{
    var info = new FileInfo(file);
    var session = Path.GetFileNameWithoutExtension(file);
    // Claude Code có lúc chạm file transcript cũ (mtime mới) mà không có tin nào ⇒ "hoạt động lúc" = timestamp của tin user/assistant cuối.
    var activeAt = DateTime.UtcNow - info.LastWriteTimeUtc < TimeSpan.FromHours(3) ? LastMessageAt(file) ?? info.LastWriteTimeUtc : info.LastWriteTimeUtc;
    var running = DateTime.UtcNow - activeAt < TimeSpan.FromMinutes(2);
    var userAt = DateTime.UtcNow - activeAt < TimeSpan.FromMinutes(15) ? LastUserMessageAt(file) : null;
    return new StreamInfo(ToId(file), "Leader — phiên Claude Code", "leader-" + session[..8], "Leader", $"Phiên Leader · {info.Length / 1024 / 1024} MB transcript",
        session, null, null, null, null, info.Length, info.CreationTimeUtc, activeAt, running, false, null, null, userAt, running ? RecentActions(file) : null);
}

static DateTime? LastMessageAt(string file)
{
    foreach (var line in Tail(file, 256 * 1024).Split('\n').Reverse())
    {
        if (!line.Contains("\"timestamp\"") || TryParse(line) is not { } j || Str(j, "type") is not ("user" or "assistant")) continue;
        if (DateTime.TryParse(Str(j, "timestamp"), null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var t)) return t;
    }
    return null;
}

// Lúc User nhắn gần nhất trong phiên Leader: dòng type=user, nội dung là chuỗi, không isMeta, không bắt đầu bằng '<'
// (loại kết quả tool, <task-notification>, tin chuyển từ phiên khác). Chỉ quét 1 MB cuối.
static DateTime? LastUserMessageAt(string file)
{
    foreach (var line in Tail(file, 1024 * 1024).Split('\n').Reverse())
    {
        if (!line.Contains("\"type\":\"user\"") || TryParse(line) is not { } j || Str(j, "type") != "user") continue;
        if (j.TryGetProperty("isMeta", out var meta) && meta.ValueKind == JsonValueKind.True) continue;
        if (!j.TryGetProperty("message", out var m) || !m.TryGetProperty("content", out var c) || c.ValueKind != JsonValueKind.String) continue;
        if ((c.GetString() ?? "").TrimStart().StartsWith('<')) continue;
        return DateTime.TryParse(Str(j, "timestamp"), null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var t) ? t : null;
    }
    return null;
}

static JsonElement? TryParse(string line)
{
    line = line.Trim().TrimStart('﻿');
    if (line.Length == 0 || line[0] != '{') return null;
    try { using var d = JsonDocument.Parse(line); return d.RootElement.Clone(); } catch { return null; }
}

// Đọc từ byte offset tới hết dòng hoàn chỉnh cuối cùng; trả sự kiện đã rút gọn + offset kế tiếp.
static (List<object> Events, long Next) ReadEvents(string file, long from)
{
    var events = new List<object>();
    using var fs = OpenShared(file);
    if (from < 0 || from > fs.Length) from = 0;
    fs.Seek(from, SeekOrigin.Begin);
    var buf = new byte[fs.Length - from];
    fs.ReadExactly(buf);
    var lastNl = Array.LastIndexOf(buf, (byte)'\n');
    if (lastNl < 0) return (events, from);
    var text = Encoding.UTF8.GetString(buf, 0, lastNl + 1);
    foreach (var line in text.Split('\n'))
    {
        if (TryParse(line) is not { } j || !j.TryGetProperty("type", out var tp)) continue;
        var type = tp.GetString();
        switch (type)
        {
            case "system" when j.TryGetProperty("subtype", out var st) && st.GetString() == "init":
                events.Add(new { kind = "init", text = $"Bắt đầu — model {Str(j, "model")} · session {Str(j, "session_id")}" });
                break;
            case "assistant":
                foreach (var c in Content(j))
                {
                    var ct = Str(c, "type");
                    if (ct == "text") events.Add(new { kind = "text", text = Str(c, "text") });
                    else if (ct == "thinking" && !string.IsNullOrWhiteSpace(Str(c, "thinking"))) events.Add(new { kind = "thinking", text = Str(c, "thinking") }); // khối rỗng ⇒ bỏ
                    else if (ct == "tool_use")
                        events.Add(new { kind = "tool", name = Str(c, "name"), tid = Str(c, "id"), text = Shorten(c.TryGetProperty("input", out var inp) ? InputText(inp) : "", 4000) });
                }
                break;
            case "user" when j.TryGetProperty("message", out var um) && um.TryGetProperty("content", out var uc) && uc.ValueKind == JsonValueKind.String:
                events.Add(new { kind = "user", text = Shorten(uc.GetString() ?? "", 4000) });
                break;
            case "user":
                foreach (var c in Content(j))
                    if (Str(c, "type") == "tool_result")
                    {
                        var body = c.TryGetProperty("content", out var cc) ? ResultText(cc) : "";
                        var err = c.TryGetProperty("is_error", out var ie) && ie.ValueKind == JsonValueKind.True;
                        events.Add(new { kind = err ? "error" : "result", tid = Str(c, "tool_use_id"), text = Shorten(body, 3000) });
                    }
                break;
            case "result":
                events.Add(new
                {
                    kind = "done",
                    text = $"Kết thúc — {(j.TryGetProperty("is_error", out var e) && e.ValueKind == JsonValueKind.True ? "LỖI" : "ok")}"
                         + $" · {Num(j, "num_turns")} lượt · ${Num(j, "total_cost_usd")} · {Num(j, "duration_ms")} ms",
                });
                break;
        }
    }
    return (events, from + lastNl + 1);
}

static IEnumerable<JsonElement> Content(JsonElement j) =>
    j.TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.Array
        ? c.EnumerateArray() : [];

static string Str(JsonElement e, string name) =>
    e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

static string Num(JsonElement e, string name) =>
    e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetRawText() : "?";

// Lệnh shell / đường dẫn file hiện thẳng; còn lại in JSON gọn.
static string InputText(JsonElement input)
{
    foreach (var key in new[] { "command", "file_path", "pattern", "url", "query" })
        if (input.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
        {
            var extra = key == "command" && input.TryGetProperty("description", out var d) ? $"   # {d.GetString()}" : "";
            return v.GetString() + extra;
        }
    return input.GetRawText();
}

static string ResultText(JsonElement c) => c.ValueKind switch
{
    JsonValueKind.String => c.GetString() ?? "",
    JsonValueKind.Array => string.Join("\n", c.EnumerateArray().Select(x => Str(x, "type") == "text" ? Str(x, "text") : $"[{Str(x, "type")}]")),
    _ => c.GetRawText(),
};

static string Shorten(string s, int max) => s.Length <= max ? s : s[..max] + $"\n… (+{s.Length - max} ký tự)";

static class Html
{
    public const string Page = """
<!doctype html>
<html lang="vi">
<head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Worker board</title>
<!-- Markdown cho tài liệu, brief, lời worker; không tải được (mất mạng) ⇒ hiện chữ thường như cũ -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/marked/12.0.2/marked.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/dompurify/3.1.6/purify.min.js"></script>
<!-- Chế độ Văn phòng: three.js chỉ tải khi mở lần đầu (import động /office/office.js) -->
<script type="importmap">{ "imports": {
  "three": "https://cdn.jsdelivr.net/npm/three@0.170.0/build/three.module.js",
  "three/addons/": "https://cdn.jsdelivr.net/npm/three@0.170.0/examples/jsm/" } }</script>
<style>
  :root { --bg:#f4f5f7; --panel:#fff; --col:#ebecf0; --line:#dfe1e6; --text:#172b4d; --muted:#5e6c84; --accent:#5b52e8;
          --tool:#0e7490; --res:#374151; --err:#b42318; --think:#8b8b95; --ok:#127a3a; --warn:#c2410c; --shadow:0 1px 2px rgba(9,30,66,.15); }
  @media (prefers-color-scheme: dark) { :root { --bg:#0f1115; --panel:#1b1f27; --col:#151920; --line:#2b313c; --text:#e6e8eb; --muted:#9aa4b2;
          --accent:#8f88ff; --tool:#5fc4d8; --res:#c9ced6; --err:#ff7b72; --think:#7d8590; --ok:#56d37e; --warn:#f0883e; --shadow:0 1px 2px rgba(0,0,0,.5); } }
  * { box-sizing:border-box } body { margin:0; font:14px/1.45 system-ui, "Segoe UI", sans-serif; background:var(--bg); color:var(--text); }
  header { display:flex; gap:12px; align-items:center; flex-wrap:wrap; padding:10px 16px; background:var(--panel); border-bottom:1px solid var(--line); position:sticky; top:0; z-index:5 }
  header h1 { font-size:16px; margin:0 8px 0 0 }
  select { font:inherit; padding:4px 8px; border:1px solid var(--line); border-radius:6px; background:var(--bg); color:var(--text) }
  .chips { display:flex; gap:6px; flex-wrap:wrap }
  .chip { font-size:12px; padding:3px 10px; border-radius:12px; border:1px solid var(--line); background:var(--bg); color:var(--text); cursor:pointer; user-select:none }
  .chip.on { background:var(--accent); border-color:var(--accent); color:#fff }
  .sum { margin-left:auto; font-size:12px; color:var(--muted) }
  .board { display:flex; flex-direction:column; gap:10px; padding:14px 16px }
  .row { display:grid; grid-template-columns:110px minmax(0, 1fr); gap:10px; background:var(--col); border-radius:10px; padding:10px }
  .row h2 { font-size:14px; margin:4px 0 0 } .row h2 .cnt { display:block; font-size:12px; font-weight:400; color:var(--muted); margin-top:2px }
  .lane { display:flex; flex-wrap:wrap; gap:6px; align-items:stretch; min-width:0 }
  .arrow { align-self:center; color:var(--muted) }
  .card { width:240px; background:var(--panel); border-radius:8px; padding:9px 10px; box-shadow:var(--shadow); cursor:pointer; border-left:4px solid transparent }
  .verdict { font-size:11px; font-weight:700; padding:1px 7px; border-radius:10px; color:#fff; margin-left:auto }
  .verdict.pass { background:var(--ok) } .verdict.fail { background:var(--err) }
  .card.stale { border-left-color:var(--warn) } .dot.stale { background:var(--warn) } .stale-m { color:var(--warn) }
  .warn { font-size:11px; font-weight:700; padding:1px 7px; border-radius:10px; color:#fff; background:var(--warn); display:inline-block; margin-top:4px }
  .blog { margin:14px 16px 0; background:var(--col); border-radius:10px; padding:8px 12px }
  .blog summary { cursor:pointer; font-weight:600; font-size:13px } .blog summary .m { font-weight:400; color:var(--muted) }
  .blog ol { margin:8px 0 2px; padding:0; list-style:none; max-height:320px; overflow:auto; font:12px/1.5 ui-monospace, Consolas, monospace }
  .blog li { padding:3px 0; border-top:1px solid var(--line); overflow-wrap:anywhere }
  .tabs { display:flex; gap:4px; padding:8px 14px 0; border-bottom:1px solid var(--line); flex-wrap:wrap }
  .tab { font:inherit; font-size:12.5px; border:1px solid var(--line); border-bottom:0; background:var(--bg); color:var(--muted); padding:4px 10px; border-radius:6px 6px 0 0; cursor:pointer }
  .tab.on { background:var(--panel); color:var(--text); font-weight:600 }
  #pane { overflow:auto; padding:12px 14px; flex:1 }
  .doc { white-space:pre-wrap; word-break:break-word; font:12.5px/1.5 ui-monospace, Consolas, monospace; margin:0 }
  .files td { padding:3px 10px 3px 0; font:12.5px ui-monospace, Consolas, monospace; overflow-wrap:anywhere } .files td.n { color:var(--muted); text-align:right }
  input[type=search] { font:inherit; font-size:13px; padding:3px 8px; border:1px solid var(--line); border-radius:6px; background:var(--bg); color:var(--text); width:160px }
  .ev.tool { cursor:pointer } .ev.tool:hover { border-color:var(--tool) }
  #office { position:fixed; left:0; right:0; bottom:0; top:53px; background:#20242c; overflow:hidden }
  #office .msg { position:absolute; inset:0; display:flex; align-items:center; justify-content:center; color:#c9ced6; font-size:14px; text-align:center; padding:20px; white-space:pre-line } #office .msg[hidden] { display:none }
  .md { white-space:normal; line-height:1.55; overflow-wrap:anywhere } .md > :first-child { margin-top:0 } .md > :last-child { margin-bottom:0 }
  .md h1 { font-size:20px } .md h2 { font-size:17px; border-bottom:1px solid var(--line); padding-bottom:4px } .md h3 { font-size:15px } .md h4 { font-size:14px }
  .md h1, .md h2, .md h3, .md h4 { margin:18px 0 8px } .md p, .md ul, .md ol, .md table, .md pre, .md blockquote { margin:0 0 10px }
  .md ul, .md ol { padding-left:22px } .md li { margin:2px 0 }
  .md code { font:12.5px ui-monospace, Consolas, monospace; background:var(--col); padding:1px 4px; border-radius:4px }
  .md pre { background:var(--col); padding:8px 10px; border-radius:6px; overflow:auto } .md pre code { background:none; padding:0 }
  .md table { border-collapse:collapse; display:block; overflow:auto; max-width:100% }
  .md th, .md td { border:1px solid var(--line); padding:4px 8px; text-align:left; vertical-align:top } .md th { background:var(--col) }
  .md blockquote { border-left:3px solid var(--line); padding-left:10px; color:var(--muted) }
  .md a { color:var(--accent) } .md hr { border:0; border-top:1px solid var(--line) }
  .ev .md { margin-top:2px } .blog li .md p { margin:0 }
  .last { font:11.5px ui-monospace, Consolas, monospace; color:var(--tool); margin-top:4px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis }
  .card:hover { outline:2px solid var(--accent) } .card.sel { outline:2px solid var(--accent) }
  .card.run { border-left-color:var(--ok) } .card.err { border-left-color:var(--err) }
  .card .t { font-weight:600; display:flex; gap:6px; align-items:center }
  .card .m { font-size:12px; color:var(--muted); margin-top:3px }
  .tag { font-size:11px; font-weight:700; padding:1px 7px; border-radius:10px; background:var(--bg); border:1px solid var(--line); color:var(--muted) }
  .dot { display:inline-block; width:8px; height:8px; border-radius:50%; background:var(--muted); flex:0 0 auto }
  .dot.run { background:var(--ok); animation:p 1.2s infinite } .dot.err { background:var(--err) }
  @keyframes p { 50% { opacity:.3 } }
  .role { display:inline-block; font-size:11px; font-weight:700; padding:1px 8px; border-radius:10px; color:#fff }
  .role.Dev { background:#2563eb } .role.BA\/SA { background:#7c3aed } .role.Reviewer { background:#d97706 } .role.Leader { background:#be185d } .role.End, .role.Khác { background:#6b7280 }
  .drawer { position:fixed; top:0; right:0; bottom:0; width:min(760px, 100%); background:var(--panel); border-left:1px solid var(--line); box-shadow:-6px 0 24px rgba(0,0,0,.18);
            display:none; flex-direction:column; z-index:10 }
  .drawer.open { display:flex }
  .dbar { display:flex; gap:12px; align-items:center; padding:10px 14px; border-bottom:1px solid var(--line); flex-wrap:wrap }
  .dbar .t { font-weight:600; margin-right:auto; min-width:0; overflow-wrap:anywhere } .dbar label { font-size:13px; color:var(--muted) }
  .x { border:1px solid var(--line); background:var(--bg); color:var(--text); font:inherit; font-size:13px; padding:4px 12px; border-radius:6px; cursor:pointer; flex:0 0 auto }
  .x:hover { border-color:var(--accent) }
  .backdrop { position:fixed; inset:0; background:rgba(0,0,0,.25); display:none; z-index:9 }
  .backdrop.open { display:block }
  #log { overflow:auto; padding:12px 14px; flex:1 }
  .ev { margin:0 0 8px; padding:8px 10px; border-radius:6px; background:var(--bg); border:1px solid var(--line); white-space:pre-wrap; word-break:break-word }
  .ev .k { font-size:11px; font-weight:700; text-transform:uppercase; margin-right:6px }
  .text .k { color:var(--accent) } .tool .k { color:var(--tool) } .tool { font-family:ui-monospace, Consolas, monospace; font-size:12.5px }
  .result, .error { font-family:ui-monospace, Consolas, monospace; font-size:12px; color:var(--res); max-height:220px; overflow:auto }
  .error { border-color:var(--err) } .error .k { color:var(--err) } .thinking { color:var(--think); font-style:italic }
  .user { border-color:#be185d } .user .k { color:#be185d }
  .done, .init { background:transparent; border-style:dashed; color:var(--muted) }
  .empty { color:var(--muted); padding:20px 6px; text-align:center; font-size:12px }
  @media (max-width: 760px) { .row { grid-template-columns:minmax(0, 1fr) } .card { width:100%; min-width:0 } .arrow { display:none } select { max-width:100% } }
</style>
</head>
<body>
<header>
  <h1>Worker board</h1>
  <select id="batch" aria-label="Đợt"></select>
  <div class="chips" id="phases"></div>
  <span class="sum" id="sum"></span>
  <button type="button" class="x" id="viewToggle" title="Chuyển giữa bảng thẻ và văn phòng 3D">🏢 Văn phòng</button>
  <button type="button" class="x" id="bell" title="Báo khi một lượt xong hoặc có kết luận mới (trình duyệt hỏi quyền lần đầu)">🔔 Thông báo: tắt</button>
</header>
<details class="blog" id="blog"><summary>Nhật ký đợt <span class="m" id="blogPath"></span></summary><ol id="blogLines"></ol></details>
<div class="board" id="board"></div>
<div id="office" hidden></div>

<div class="backdrop" id="backdrop"></div>
<aside class="drawer" id="drawer" aria-label="Chi tiết lượt">
  <div class="dbar">
    <button type="button" class="x" id="close" aria-label="Đóng">Đóng ✕</button>
    <span class="t" id="title"></span>
    <label title="Chỉ hiện khi stream có ghi nội dung suy nghĩ"><input type="checkbox" id="fThink"> suy nghĩ (nếu có)</label>
    <label><input type="checkbox" id="fRes" checked> kết quả lệnh</label>
    <label><input type="checkbox" id="fNewest" checked> mới nhất lên đầu</label>
    <input type="search" id="q" placeholder="Tìm trong nhật ký…" aria-label="Tìm trong nhật ký">
    <button type="button" class="x" id="copySid" title="Copy session id">Copy session</button>
  </div>
  <div class="tabs" id="tabs"></div>
  <div id="log"></div>
  <div id="pane" hidden></div>
</aside>

<script>
let all = [], batches = [], sel = null, offset = 0, timer = null, batch = null, phase = 'Tất cả';
try { batch = localStorage.getItem('wb-batch'); phase = localStorage.getItem('wb-phase') || 'Tất cả'; } catch {}
batch = new URLSearchParams(location.search).get('batch') || batch; // ?batch=<đợt> để mở thẳng một đợt
const $ = id => document.getElementById(id);
const esc = s => String(s).replace(/[&<>"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
const ago = iso => { const s = (Date.now() - new Date(iso)) / 1000; return s < 60 ? Math.round(s)+'s' : s < 3600 ? Math.round(s/60)+' phút' : s < 86400 ? Math.round(s/3600)+' giờ' : Math.round(s/86400)+' ngày'; };
const roleBadge = r => `<span class="role ${r.split(' ')[0]}">${esc(r)}</span>`;
const kinds = { text:'nói', tool:'lệnh', result:'kết quả', error:'lỗi', thinking:'nghĩ', user:'người dùng', done:'', init:'' };
const save = (k, v) => { try { localStorage.setItem(k, v); } catch {} };
// Markdown ⇒ HTML đã lọc (DOMPurify). Thư viện chưa tải được ⇒ chữ thường giữ xuống dòng.
const hasMd = () => typeof marked !== 'undefined' && typeof DOMPurify !== 'undefined';
const md = (s, inline) => hasMd()
  ? `<div class="md">${DOMPurify.sanitize(inline ? marked.parseInline(s || '', { gfm:true }) : marked.parse(s || '', { gfm:true, breaks:true }))}</div>`
  : `<div class="doc">${esc(s || '')}</div>`;

// Phase của lượt suy từ tên: dev-p2-r3 ⇒ P2, sa-gate-p1-r2 ⇒ P1, sa-disc-r1 ⇒ Discovery.
function phaseOf(i) {
  if (i.role === 'Leader') return 'Điều phối';
  const m = i.name.match(/-p(\d+)(?:-|$)/); if (m) return 'P' + m[1];
  if (/disc|mockup/.test(i.name)) return 'Discovery';
  return 'Khác';
}
// Thứ tự hàng: Discovery → P1, P2… → Khác → Điều phối.
const phaseRank = p => p === 'Discovery' ? -1 : p[0] === 'P' ? +p.slice(1) : p === 'Khác' ? 900 : 1000;
const t = iso => +new Date(iso);
// Tên dễ đọc: sa-gate-p2-r2 ⇒ "SA gate P2 · vòng 2"
function label(i) {
  if (i.role === 'Leader') return 'Phiên Leader ' + i.name.slice(7);
  let n = i.name.replace(/^sa-/, 'SA ').replace(/^dev-/, 'Dev ').replace(/^reviewer-/, 'Review ')
    .replace(/-p(\d+)/, ' P$1').replace(/-r(\d+)$/, ' · vòng $1').replace(/-cp(\d+)/, ' CP$1').replace(/-fix(\d+)/, ' · sửa $1').replace(/-/g, ' ');
  return n.replace(/\bgate\b/, 'gate').replace(/\bdisc\b/, 'discovery');
}

async function load() {
  all = await (await fetch('/api/streams')).json();
  // đợt có hoạt động gần nhất lên đầu (cùng ngày thì tên không nói được đợt nào mới hơn)
  const lastAct = {}; for (const i of all) if (i.role !== 'Leader') lastAct[i.batch] = Math.max(lastAct[i.batch] || 0, +new Date(i.modified));
  batches = Object.keys(lastAct).sort((a, b) => lastAct[b] - lastAct[a]);
  if (!batch || !batches.includes(batch)) batch = batches[0];
  $('batch').innerHTML = batches.map(b => `<option ${b === batch ? 'selected' : ''}>${esc(b)}</option>`).join('');
  render(); notifyChanges();
  window.dispatchEvent(new Event('wb-data'));
  if ($('blog').open) loadBatchLog();
  if (openName) { const it = all.find(i => i.batch === batch && i.name === openName); openName = null; if (it) open(it); }
}
let openName = new URLSearchParams(location.search).get('open'); // ?open=<tên lượt, vd dev-p3-r2> để mở thẳng một lượt

// Tiêu đề tab đếm lượt worker đang chạy; thông báo trình duyệt khi lượt xong hoặc có kết luận mới.
let prev = null, notifyOn = false;
try { notifyOn = localStorage.getItem('wb-notify') === '1' && Notification.permission === 'granted'; } catch {}
function notifyChanges() {
  const workers = all.filter(i => i.role !== 'Leader');
  const n = workers.filter(i => i.running).length;
  document.title = (n ? `(${n}) ` : '') + 'Worker board';
  const now = new Map(workers.map(i => [i.id, i]));
  if (prev && notifyOn)
    for (const [id, i] of now) {
      const p = prev.get(id);
      if (p && p.running && !i.running) new Notification(`Xong: ${label(i)}${i.verdict ? ' — ' + i.verdict : ''}`, { body: i.batch, tag: id });
      else if (!p && i.running) new Notification(`Bắt đầu: ${label(i)}`, { body: i.batch, tag: id });
    }
  prev = now;
}
function bellText() { $('bell').textContent = '🔔 Thông báo: ' + (notifyOn ? 'bật' : 'tắt'); }
$('bell').onclick = async () => {
  if (!notifyOn && typeof Notification !== 'undefined' && await Notification.requestPermission() !== 'granted') return;
  notifyOn = !notifyOn; save('wb-notify', notifyOn ? '1' : '0'); bellText();
};
bellText();

async function loadBatchLog() {
  const r = await fetch('/api/batchlog?batch=' + encodeURIComponent(batch));
  if (!r.ok) return;
  const j = await r.json();
  $('blogPath').textContent = j.path ? `· ${j.path} · ${j.lines.length} dòng cuối, mới nhất lên đầu` : '· đợt chưa có workflow log';
  $('blogLines').innerHTML = j.lines.slice().reverse().map(l => `<li>${md(l, true)}</li>`).join('');
}
try { $('blog').open = localStorage.getItem('wb-blog') === '1'; } catch {}
$('blog').addEventListener('toggle', () => { save('wb-blog', $('blog').open ? '1' : '0'); if ($('blog').open) loadBatchLog(); });

function render() {
  // Phiên Leader thuộc đợt nếu chạy giao với khoảng [lượt worker đầu − 6 giờ, lượt cuối + 6 giờ]; đợt mới nhất thì không chặn cuối.
  const ws = all.filter(i => i.role !== 'Leader' && i.batch === batch);
  const start = Math.min(...ws.map(i => t(i.created))) - 6 * 3600e3;
  const end = batch === batches[0] ? Infinity : Math.max(...ws.map(i => t(i.modified))) + 6 * 3600e3;
  const items = ws.concat(all.filter(i => i.role === 'Leader' && t(i.modified) >= start && t(i.created) <= end));
  const phases = ['Tất cả', ...[...new Set(items.map(phaseOf))].sort((a, b) => phaseRank(a) - phaseRank(b))];
  if (!phases.includes(phase)) phase = 'Tất cả';
  $('phases').innerHTML = phases.map(p => `<span class="chip ${p === phase ? 'on' : ''}" data-p="${esc(p)}">${esc(p)}</span>`).join('');
  document.querySelectorAll('#phases .chip').forEach(el => el.onclick = () => { phase = el.dataset.p; save('wb-phase', phase); render(); });
  const shown = items.filter(i => phase === 'Tất cả' || phaseOf(i) === phase);
  const running = shown.filter(i => i.running).length, cost = shown.reduce((s, i) => s + (i.cost || 0), 0);
  $('sum').textContent = `${shown.length} lượt · ${running} đang chạy · tổng ~$${cost.toFixed(2)}`;
  const rows = [...new Set(shown.map(phaseOf))].sort((a, b) => phaseRank(a) - phaseRank(b));
  $('board').innerHTML = rows.length ? rows.map(p => {
    const cards = shown.filter(i => phaseOf(i) === p).sort((a, b) => t(a.created) - t(b.created));
    const c = cards.reduce((s, i) => s + (i.cost || 0), 0);
    return `<section class="row"><h2>${esc(p)}<span class="cnt">${cards.length} lượt${c ? ' · $' + c.toFixed(2) : ''}</span></h2>`
      + `<div class="lane">${cards.map(card).join(p === 'Điều phối' ? '' : '<span class="arrow">→</span>')}</div></section>`; // phiên Leader song song, không nối tiếp
  }).join('') : '<div class="empty">Chưa có lượt</div>';
  document.querySelectorAll('.card').forEach(el => el.addEventListener('click', () => {
    if (sel && sel.id === el.dataset.id) closeDrawer(); else open(all.find(i => i.id === el.dataset.id));
  }));
}

function card(i) {
  // Lượt worker không còn chạy mà chưa có dòng result: ≤ 2 giờ ⇒ có vẻ treo; lâu hơn ⇒ đã chết/bị dừng giữa chừng.
  const stale = i.role !== 'Leader' && !i.running && !i.ended;
  const state = i.running ? 'run' : stale ? 'stale' : i.isError ? 'err' : '';
  const staleText = stale ? (Date.now() - t(i.modified) < 2 * 3600e3 ? `có vẻ treo — ${ago(i.modified)} không có sự kiện` : 'dừng, không có kết quả') : '';
  return `<div class="card ${state} ${sel && sel.id === i.id ? 'sel' : ''}" data-id="${i.id}">
    <div class="t"><span class="dot ${state}"></span>${roleBadge(i.role)}${i.verdict ? `<span class="verdict ${i.verdict === 'PASS' ? 'pass' : 'fail'}">${esc(i.verdict)}</span>` : ''}</div>
    <div class="t" style="margin-top:4px">${esc(label(i))}</div>
    <div class="m">${i.running ? 'đang chạy · ' : ''}${stale ? `<span class="stale-m">${esc(staleText)}</span>` : ago(i.modified) + ' trước'}${i.cost != null ? ' · $' + i.cost.toFixed(2) : ''}${i.turns != null ? ' · ' + i.turns + ' lượt' : ''}</div>
    ${i.warn ? `<span class="warn" title="Vai này không được sửa code — mở tab 'File đã sửa'">⚠ ${esc(i.warn)}</span>` : ''}
    ${i.last ? `<div class="last" title="${esc(i.last)}">${esc(i.last)}</div>` : ''}
  </div>`;
}

function open(item) {
  sel = item; offset = 0; $('log').innerHTML = '';
  $('fNewest').checked = !!item.running; // đang chạy: theo dõi cái mới nhất; đã xong: đọc từ đầu
  $('title').innerHTML = roleBadge(item.role) + ' ' + esc(label(item)) + `<div class="m" style="font-weight:400;font-size:12px;color:var(--muted)">${esc(item.brief || item.name)}${item.session ? ' · ' + esc(item.session) : ''}</div>`;
  $('drawer').classList.add('open'); $('backdrop').classList.add('open');
  $('copySid').hidden = !item.session; $('q').value = '';
  showTab([['log', 'Nhật ký']], 'log');
  clearTimeout(timer); poll(); render();
  if (item.role !== 'Leader') loadDetail(item);
}

// Tab trong khung chi tiết: Nhật ký · Brief · từng tài liệu được nhắc tới · File đã sửa.
let tabs = [], detail = null;
function showTab(list, key) {
  if (list) tabs = list;
  $('tabs').innerHTML = tabs.map(([k, name]) => `<button type="button" class="tab ${k === key ? 'on' : ''}" data-k="${esc(k)}">${esc(name)}</button>`).join('');
  document.querySelectorAll('#tabs .tab').forEach(b => b.onclick = () => showTab(null, b.dataset.k));
  const isLog = key === 'log';
  $('log').hidden = !isLog; $('pane').hidden = isLog;
  document.querySelectorAll('.dbar label, #q').forEach(el => el.style.visibility = isLog ? '' : 'hidden');
  if (isLog) return;
  $('pane').scrollTop = 0; $('pane').dataset.doc = '';
  if (key === 'brief') $('pane').innerHTML = md(detail.brief);
  else if (key === 'files') $('pane').innerHTML = detail.touched.length
    ? `<p class="m">Đếm theo tool Edit/Write — file ghi bằng lệnh shell không có ở đây.</p><table class="files">${detail.touched.map(f => `<tr><td class="n">${f.count}×</td><td>${esc(f.path)}</td></tr>`).join('')}</table>`
    : '<div class="empty">Lượt này không sửa file nào qua Edit/Write.</div>';
  else {
    $('pane').innerHTML = '<div class="empty">Đang tải…</div>';
    fetch('/api/doc?path=' + encodeURIComponent(key)).then(r => r.ok ? r.text() : 'Không đọc được file.')
      .then(x => {
        if ($('tabs').querySelector('.tab.on')?.dataset.k !== key) return;
        $('pane').innerHTML = `<p class="m">project-docs/${esc(key)}</p>` + (key.endsWith('.md') ? md(x) : `<pre class="doc">${esc(x)}</pre>`);
        $('pane').dataset.doc = key;
        $('pane').querySelectorAll('a[href^="http"]').forEach(a => { a.target = '_blank'; a.rel = 'noopener'; });
      });
  }
}
// Link .md tương đối trong tài liệu ⇒ mở thành tab mới trong khung chi tiết (không rời trang).
$('pane').addEventListener('click', e => {
  const a = e.target.closest('a[href]');
  if (!a || !$('pane').dataset.doc) return;
  const href = a.getAttribute('href');
  if (/^[a-z]+:|^#/i.test(href)) return;
  e.preventDefault();
  const path = decodeURIComponent(new URL(href, 'http://x/' + $('pane').dataset.doc).pathname.slice(1));
  if (!path.endsWith('.md')) return;
  if (!tabs.some(([k]) => k === path)) tabs.push([path, path.split('/').pop().replace(/^\d{6}-/, '').replace(/\.md$/, '')]);
  showTab(null, path);
});
async function loadDetail(item) {
  const r = await fetch('/api/detail?id=' + item.id);
  if (!r.ok || !sel || sel.id !== item.id) return;
  detail = await r.json();
  const list = [['log', 'Nhật ký']];
  if (detail.brief) list.push(['brief', 'Brief']);
  detail.docs.forEach(d => list.push([d, d.split('/').pop().replace(/^\d{6}-/, '').replace(/\.md$/, '')]));
  list.push(['files', `File đã sửa (${detail.touched.length})`]);
  let on = $('tabs').querySelector('.tab.on')?.dataset.k || 'log';
  if (openTab) { const m = list.find(([k, n]) => k === openTab || n === openTab); openTab = null; if (m) on = m[0]; } // ?tab=brief | files | epic1-phase3 …
  showTab(list, on);
}
let openTab = new URLSearchParams(location.search).get('tab');
$('copySid').onclick = async () => {
  if (!sel?.session) return;
  try { await navigator.clipboard.writeText(sel.session); $('copySid').textContent = 'Đã copy ✓'; } catch { $('copySid').textContent = 'Không copy được'; }
  setTimeout(() => $('copySid').textContent = 'Copy session', 1500);
};
function closeDrawer() { $('drawer').classList.remove('open'); $('backdrop').classList.remove('open'); sel = null; clearTimeout(timer); render(); }
$('close').addEventListener('click', closeDrawer);
$('backdrop').addEventListener('click', closeDrawer);
document.addEventListener('keydown', e => { if (e.key === 'Escape') closeDrawer(); });

async function poll() {
  if (!sel) return;
  const id = sel.id;
  const r = await (await fetch(`/api/events?id=${id}&from=${offset}`)).json();
  if (!sel || sel.id !== id) return;
  const first = offset === 0;
  offset = r.next;
  const log = $('log'), newest = $('fNewest').checked;
  for (const e of r.events) {
    const d = document.createElement('div');
    d.className = 'ev ' + e.kind; d.dataset.kind = e.kind;
    if (e.tid) d.dataset.tid = e.tid;
    if (e.kind === 'tool') { d.title = 'Bấm để hiện/ẩn kết quả của lệnh này'; d.onclick = () => toggleResult(e.tid); }
    d.innerHTML = (kinds[e.kind] ? `<span class="k">${kinds[e.kind]}${e.name ? ' · ' + esc(e.name) : ''}</span>` : '')
      + (e.kind === 'text' || e.kind === 'thinking' ? md(e.text) : esc(e.text || '')); // lời worker là markdown; lệnh/kết quả giữ nguyên văn
    if (e.kind === 'text' || e.kind === 'thinking') d.style.whiteSpace = 'normal';
    if (newest) log.prepend(d); else log.appendChild(d);
  }
  applyFilters();
  if (r.events.length) log.scrollTop = newest || (first && !r.running) ? 0 : log.scrollHeight; // lượt đã xong: mở ở đầu để đọc từ đầu
  timer = setTimeout(poll, r.running ? 2000 : 15000);
}
// Kết quả lệnh bị ẩn theo bộ lọc vẫn hiện được từng cái bằng cách bấm vào lệnh (data-force).
function toggleResult(tid) {
  if (!tid) return;
  document.querySelectorAll(`.ev[data-tid="${CSS.escape(tid)}"]:not(.tool)`).forEach(r => r.dataset.force = r.dataset.force === '1' ? '' : '1');
  applyFilters();
}
function applyFilters() {
  const q = $('q').value.trim().toLowerCase();
  document.querySelectorAll('.ev').forEach(d => {
    const k = d.dataset.kind;
    const hiddenByKind = (k === 'thinking' && !$('fThink').checked) || (k === 'result' && !$('fRes').checked && d.dataset.force !== '1');
    const miss = q && !d.textContent.toLowerCase().includes(q);
    d.style.display = hiddenByKind || miss ? 'none' : '';
  });
}
['fThink','fRes'].forEach(id => $(id).onchange = applyFilters);
$('q').oninput = applyFilters;
$('fNewest').onchange = () => { const log = $('log'); [...log.children].reverse().forEach(c => log.appendChild(c)); log.scrollTop = $('fNewest').checked ? 0 : log.scrollHeight; };
$('batch').onchange = () => { batch = $('batch').value; save('wb-batch', batch); render(); if ($('blog').open) loadBatchLog(); };
// API cho chế độ Văn phòng (office.js): đọc dữ liệu, mở khung chi tiết. Sự kiện 'wb-data' sau mỗi lần tải.
window.wb = { items: () => all, batches: () => batches, batch: () => batch, open: i => open(i), label, phaseOf };
let officeOn = false;
function setView(on) {
  officeOn = on; save('wb-view', on ? 'office' : 'board');
  $('office').hidden = !on; $('board').hidden = on; $('blog').hidden = on;
  $('viewToggle').textContent = on ? '📋 Bảng' : '🏢 Văn phòng';
  $('office').style.top = document.querySelector('header').offsetHeight + 'px';
  if (on) import('/office/office.js').then(m => m.show($('office'))).catch(e => { $('office').innerHTML = `<div class="msg">Không tải được văn phòng 3D: ${esc(e.message)}</div>`; });
  else import('/office/office.js').then(m => m.hide()).catch(() => {});
}
$('viewToggle').onclick = () => setView(!officeOn);
window.addEventListener('resize', () => { $('office').style.top = document.querySelector('header').offsetHeight + 'px'; });
try { if ((new URLSearchParams(location.search).get('view') || localStorage.getItem('wb-view')) === 'office') setView(true); } catch {}
load(); setInterval(load, 5000);
</script>
</body>
</html>
""";
}

record PhaseInfo(string Batch, string Phase, string? Title, List<(string Cp, string Text)> Plan, string? Cp, bool CpFromCode, StreamInfo? Run, string? Says,
    List<string> Epic, List<StreamInfo> InPhase);

record StreamInfo(string id, string batch, string name, string role, string? brief, string? session, string? model, double? cost, int? turns, bool? isError, long size,
    DateTime created, DateTime modified, bool running, bool ended, string? verdict, string? warn, DateTime? userAt, List<string>? recent)
{
    public string? last => recent?[^1];                   // việc cuối — hiện trên thẻ
}
