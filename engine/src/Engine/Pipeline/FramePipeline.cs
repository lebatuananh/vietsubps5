using System.Text.Json;
using Engine.Config;
using Engine.Geometry;
using Engine.Layout;
using Engine.Protocol;
using Engine.Translation;

namespace Engine.Pipeline;

public sealed record SourceFrame(byte[] Bgra, int Width, int Height, long TimestampMs, string? Name)
{
    public int Stride => Width * 4;
}

public interface IOcrProvider
{
    Task<IReadOnlyList<OcrLineData>> RecognizeAsync(SourceFrame frame, CancellationToken ct);
}

/// <summary>N5: hộp lớn nhất quanh bbox mà nền còn đồng nhất với màu che, ≤ maxFactor× bbox mỗi chiều, không lấn bbox khác.</summary>
public static class RoomFinder
{
    public static Box Find(byte[] bgra, int width, int height, int stride, Box bbox, uint coverArgb,
        IReadOnlyList<Box> others, double maxFactor, int tolerance = 24)
    {
        int cr = (int)((coverArgb >> 16) & 0xFF), cg = (int)((coverArgb >> 8) & 0xFF), cb = (int)(coverArgb & 0xFF);
        int l = (int)Math.Floor(bbox.X), t = (int)Math.Floor(bbox.Y), r = (int)Math.Ceiling(bbox.X + bbox.W), b = (int)Math.Ceiling(bbox.Y + bbox.H);
        int maxW = (int)(bbox.W * maxFactor), maxH = (int)(bbox.H * maxFactor);
        const int step = 4;

        bool Close(int x, int y)
        {
            int i = y * stride + x * 4;
            return Math.Abs(bgra[i + 2] - cr) + Math.Abs(bgra[i + 1] - cg) + Math.Abs(bgra[i] - cb) <= tolerance;
        }
        bool Hits(int x0, int y0, int x1, int y1)
        {
            foreach (var o in others)
                if (x0 < o.X + o.W && x1 > o.X && y0 < o.Y + o.H && y1 > o.Y) return true;
            return false;
        }
        bool StripOk(int x0, int y0, int x1, int y1)
        {
            if (x0 < 0 || y0 < 0 || x1 > width || y1 > height || x1 <= x0 || y1 <= y0) return false;
            if (Hits(x0, y0, x1, y1)) return false;
            int total = 0, ok = 0;
            for (int y = y0; y < y1; y += 2) for (int x = x0; x < x1; x += 2) { total++; if (Close(x, y)) ok++; }
            return total > 0 && ok * 100 >= total * 95;
        }

        bool grew = true;
        while (grew)
        {
            grew = false;
            if (r - l + step <= maxW && StripOk(r, t, r + step, b)) { r += step; grew = true; }
            if (r - l + step <= maxW && StripOk(l - step, t, l, b)) { l -= step; grew = true; }
            if (b - t + step <= maxH && StripOk(l, b, r, b + step)) { b += step; grew = true; }
            if (b - t + step <= maxH && StripOk(l, t - step, r, t)) { t -= step; grew = true; }
        }
        return new Box(l, t, r - l, b - t);
    }
}

public sealed record BuiltFrame(IReadOnlyList<OverlayItem> Items, IReadOnlyList<NormalizedSource> Missing, IReadOnlyList<LayoutBlock> MissingBlocks);

public static class ItemBuilder
{
    /// <summary>Dựng item cho các block đã có bản dịch (D5: câu chưa có bản dịch KHÔNG vẽ — vào Missing).</summary>
    public static BuiltFrame Build(SourceFrame f, ContentRect content, IReadOnlyList<LayoutBlock> blocks, TranslationService svc, EngineConfig cfg)
    {
        var items = new List<OverlayItem>();
        var missing = new List<NormalizedSource>();
        var missingBlocks = new List<LayoutBlock>();
        // C1 (không để điểm ảnh gốc ló ra): hộp OCR thường hụt vài px so với nét chữ thật ⇒ nới mỗi phía BoxPadPx (quy theo cao khung).
        double pad = Math.Ceiling(cfg.Tuning.BoxPadPx * content.Height / 1080.0);
        Box Pad(Box r)
        {
            double x1 = Math.Max(0, r.X - pad), y1 = Math.Max(0, r.Y - pad);
            double x2 = Math.Min(f.Width, r.X + r.W + pad), y2 = Math.Min(f.Height, r.Y + r.H + pad);
            return new Box(x1, y1, x2 - x1, y2 - y1);
        }
        var padded = blocks.Select(b => Pad(b.BoxPx)).ToList();

        // B1: id duy nhất trong gói — đếm lần xuất hiện của cặp (ô, câu) theo thứ tự đọc (blocks đã xếp y rồi x).
        var seen = new Dictionary<string, int>();

        for (int bi = 0; bi < blocks.Count; bi++)
        {
            var b = blocks[bi];
            var ns = SourceNormalizer.Normalize(b.Text);
            string baseId = ItemId.Compute(b.Box01, ns.Key);
            seen.TryGetValue(baseId, out int occurrence); seen[baseId] = occurrence + 1;
            var tr = svc.Lookup(ns.Key);
            if (tr is null) { missing.Add(ns); missingBlocks.Add(b); continue; }
            var boxPx = padded[bi];
            var box01 = Normalizer.Normalize(boxPx, content);

            string vi = SourceNormalizer.Restore(tr.Text, ns.Slots);
            string? alt = tr.ShortVi is null ? null : SourceNormalizer.Restore(tr.ShortVi, ns.Slots);
            if (IsAllUpper(b.Text)) { vi = vi.ToUpperInvariant(); alt = alt?.ToUpperInvariant(); }

            var colors = ColorSampler.Sample(f.Bgra, f.Width, f.Height, f.Stride, boxPx, cfg.Tuning.SampleRimPx);
            string mode = ColorSampler.DecideCoverMode(cfg.Display.CoverMode, colors.RimSpread, cfg.Tuning.CoverUniformThreshold);

            Box? room = null;
            if (mode != "none")
            {
                var others = padded.Where((o, k) => k != bi).ToList();
                var rpx = RoomFinder.Find(f.Bgra, f.Width, f.Height, f.Stride, boxPx, colors.Cover, others, cfg.Tuning.RoomMaxFactor);
                room = Normalizer.Normalize(rpx, content);
            }

            if (vi.Length > ProtocolLimits.MaxTextChars) vi = vi[..ProtocolLimits.MaxTextChars];
            if (alt is not null && alt.Length > ProtocolLimits.MaxTextChars) alt = alt[..ProtocolLimits.MaxTextChars];
            items.Add(new OverlayItem(
                occurrence == 0 ? baseId : ItemId.Compute(b.Box01, ns.Key, occurrence), box01, vi, alt, b.FontPx, b.Group, b.MaxLines, "start", false,
                ColorSampler.Hex(colors.Text), new CoverSpec(mode, ColorSampler.Hex(colors.Cover)), room));
        }
        // NB10: TV bỏ CẢ gói nếu > 200 item ⇒ giữ 200 item đầu (trên xuống).
        if (items.Count > cfg.Tuning.MaxItemsPerFrame) items = items.Take(cfg.Tuning.MaxItemsPerFrame).ToList();
        return new BuiltFrame(items, missing, missingBlocks);
    }

    private static bool IsAllUpper(string s)
    {
        int letters = s.Count(char.IsLetter);
        return letters >= 2 && !s.Any(char.IsLower);
    }
}

/// <summary>
/// Vòng xử lý dùng chung cho <c>run</c> và <c>replay</c> (nguồn khung là interface): chữ ký ô lưới → Động/Đứng yên →
/// OCR → bố cục → dịch (một phần) → gói. Thời gian lấy từ <see cref="SourceFrame.TimestampMs"/> (replay dùng đồng hồ ảo).
/// </summary>
public sealed class FramePipeline(
    EngineConfig cfg, IOcrProvider ocr, TranslationService svc, IFrameSink sink, string sessionId,
    Action<string>? log = null, bool verbose = false)
{
    private readonly MotionTracker _motion = new(cfg.Tuning);
    private ContentRect _content = new(0, 0, 0, 0);
    private List<OverlayItem> _drawn = new();
    private SourceFrame? _stableFrame;
    private IReadOnlyList<LayoutBlock> _blocks = [];
    private long _lastSendMs = long.MinValue / 2;
    private long _seq;
    private bool _matchAnnounced;
    private int _epoch;
    private Task _translation = Task.CompletedTask;
    private volatile bool _rebuild;
    private int _rebuildEpoch;

    public OverlayFrame? LastSent { get; private set; }
    public long SentCount => _seq;
    public int ItemSetChanges { get; private set; }
    public IReadOnlyList<NormalizedSource> LastMissing { get; private set; } = [];
    public MotionState State { get; private set; } = MotionState.Moving;
    private string _lastItemSetKey = "";

    public Task WaitTranslationsAsync() => _translation;

    public async Task OnFrameAsync(SourceFrame f, CancellationToken ct)
    {
        if (_content.Width == 0 || _content.Width > f.Width || _content.Height > f.Height) _content = new ContentRect(0, 0, f.Width, f.Height);
        var protectedCells = _drawn.SelectMany(i => FrameSignature.CellsOf(i.Box)).ToHashSet();
        var textChangedCells = UpdateTextSignatures(f);
        var sig = FrameSignature.Compute(f.Bgra, f.Width, f.Height, f.Stride, _content);
        var u = _motion.Update(f.TimestampMs, sig, protectedCells, textChangedCells);
        State = u.State;

        if (u.MatchMode)
        {
            if (!_matchAnnounced) { log?.Invoke("[run] chế độ trận: >50% ô đổi — im lặng, không OCR/dịch tới khi đứng yên ≥ 1,5 s"); _matchAnnounced = true; _epoch++; }
            if (_drawn.Count > 0) { _drawn = new(); Send(f.TimestampMs); }
            return;
        }
        _matchAnnounced = false;

        if (u.State == MotionState.Moving)
        {
            if (u.BecameMoving) { _epoch++; if (verbose) log?.Invoke($"[run] t={f.TimestampMs} Động ({u.ChangedCells.Count} ô đổi)"); }
            if (u.ChangedCells.Count > 0)
            {
                var changed = u.ChangedCells.ToHashSet();
                // Gỡ NGAY item giao các ô đổi (N3); các item còn lại giữ nguyên.
                var keep = _drawn.Where(i => !FrameSignature.CellsOf(i.Box).Any(changed.Contains)).ToList();
                if (keep.Count != _drawn.Count) { _drawn = keep; Send(f.TimestampMs); }
            }
            return;
        }

        // Đứng yên
        if (u.BecameStable || _motion.DirtySinceAck.Count > 0)
        {
            if (verbose) log?.Invoke($"[run] t={f.TimestampMs} Đứng yên → OCR (ô bẩn {_motion.DirtySinceAck.Count}, nền động {_motion.BackgroundCells.Count})");
            await ProcessStableAsync(f, ct);
            _motion.Ack();
        }
        else if (_rebuild && _rebuildEpoch == _epoch)
        {
            _rebuild = false;
            RebuildFromBlocks(f, sendAlways: false);
        }
        else if (f.TimestampMs - _lastSendMs >= cfg.Tuning.HeartbeatMs)
        {
            Send(f.TimestampMs); // nhịp tim: gói cũ, seq mới
        }
    }

    // B3: so sánh riêng vùng chữ (bbox OCR gốc = box item co lại BoxPadPx) của từng item đang vẽ giữa hai khung liên tiếp.
    private readonly Dictionary<string, byte[]> _textSigs = new();

    private HashSet<int> UpdateTextSignatures(SourceFrame f)
    {
        var changedCells = new HashSet<int>();
        var alive = new HashSet<string>();
        double pad = Math.Ceiling(cfg.Tuning.BoxPadPx * _content.Height / 1080.0);
        foreach (var it in _drawn)
        {
            alive.Add(it.Id);
            var r = new Box(_content.X + it.Box.X * _content.Width + pad, _content.Y + it.Box.Y * _content.Height + pad,
                            Math.Max(1, it.Box.W * _content.Width - 2 * pad), Math.Max(1, it.Box.H * _content.Height - 2 * pad));
            var now = FrameSignature.RegionSignature(f.Bgra, f.Width, f.Height, f.Stride, r);
            if (_textSigs.TryGetValue(it.Id, out var prev))
            {
                bool changed = false;
                for (int k = 0; k < now.Length && !changed; k++) changed = Math.Abs(now[k] - prev[k]) > cfg.Tuning.CellDiffThreshold;
                if (changed) foreach (var c in FrameSignature.CellsOf(it.Box)) changedCells.Add(c);
            }
            _textSigs[it.Id] = now;
        }
        foreach (var k in _textSigs.Keys.Where(k => !alive.Contains(k)).ToList()) _textSigs.Remove(k);
        return changedCells;
    }

    private async Task ProcessStableAsync(SourceFrame f, CancellationToken ct)
    {
        var det = ContentRectDetector.Detect(f.Bgra, f.Width, f.Height, f.Stride);
        _content = det.RectForNormalization(f.Width, f.Height);
        var lines = await ocr.RecognizeAsync(f, ct);
        _blocks = LayoutBuilder.Build(lines, _content, cfg.Tuning, cfg.Display.FontScale);
        _stableFrame = f;
        RebuildFromBlocks(f, sendAlways: true);
    }

    private void RebuildFromBlocks(SourceFrame f, bool sendAlways)
    {
        var src = _stableFrame ?? f;
        var built = ItemBuilder.Build(src, _content, _blocks, svc, cfg);
        LastMissing = built.Missing;
        var newItems = built.Items.ToList();
        bool changed = !SameItems(_drawn, newItems);
        _drawn = newItems;
        if (changed || sendAlways) Send(f.TimestampMs);
        if (verbose) log?.Invoke($"[run] t={f.TimestampMs} {newItems.Count} item, chưa dịch {built.Missing.Count}");

        if (built.Missing.Count > 0)
        {
            int epoch = _epoch;
            // NB6: một hàng đợi dịch duy nhất — lượt sau chờ lượt trước xong (không hai Save/ProcessAsync chạy chồng).
            var prev = _translation;
            _translation = Task.Run(async () =>
            {
                try { await prev; } catch { /* lỗi của lượt trước đã được xử lý ở đó */ }
                var n = await svc.ProcessAsync(built.Missing, CancellationToken.None);
                if (n > 0) { _rebuildEpoch = epoch; _rebuild = true; } // cùng chữ ký ⇒ gửi lại gói đủ (D5)
            });
        }
    }

    private static bool SameItems(List<OverlayItem> a, List<OverlayItem> b)
        => a.Count == b.Count && a.Zip(b).All(p => p.First.Id == p.Second.Id && p.First.Text == p.Second.Text && p.First.Cover == p.Second.Cover);

    /// <summary>Gửi trạng thái hiện tại (hoặc <c>items:[]</c>) — ghi nhận số lần BỘ ITEM đổi.</summary>
    public void Send(long nowMs)
    {
        var frame = new OverlayFrame(ProtocolJson.Version, sessionId, ++_seq, cfg.Tuning.TtlMs,
            new DisplaySpec(cfg.Display.MinFontPx, cfg.Display.HoldMs), _drawn.ToList());
        LastSent = frame;
        _lastSendMs = nowMs;
        var key = string.Join('|', _drawn.Select(i => i.Id + ":" + i.Text));
        if (key != _lastItemSetKey) { ItemSetChanges++; _lastItemSetKey = key; }
        sink.Send(ProtocolJson.SerializeFrame(frame));
    }

    /// <summary>Gửi gói rỗng ngay (Ctrl+C, mất cửa sổ).</summary>
    public void SendEmpty(long nowMs) { _drawn = new(); Send(nowMs); }
}
