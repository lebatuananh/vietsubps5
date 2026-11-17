using Engine.Config;
using Engine.Geometry;

namespace Engine.Pipeline;

/// <summary>Chữ ký khung theo ô lưới 16×9 trên contentRect (N3): trung bình độ xám mỗi ô.</summary>
public static class FrameSignature
{
    public const int Cols = 16;
    public const int Rows = 9;
    public const int Cells = Cols * Rows;

    public static byte[] Compute(ReadOnlySpan<byte> bgra, int width, int height, int stride, ContentRect rect)
    {
        var sig = new byte[Cells];
        for (int r = 0; r < Rows; r++)
        for (int c = 0; c < Cols; c++)
        {
            int x0 = rect.X + c * rect.Width / Cols, x1 = rect.X + (c + 1) * rect.Width / Cols;
            int y0 = rect.Y + r * rect.Height / Rows, y1 = rect.Y + (r + 1) * rect.Height / Rows;
            x1 = Math.Min(x1, width); y1 = Math.Min(y1, height);
            long sum = 0; int n = 0;
            for (int y = y0; y < y1; y += 2)
            {
                int row = y * stride;
                for (int x = x0; x < x1; x += 2)
                {
                    int i = row + x * 4;
                    sum += (18 * bgra[i] + 183 * bgra[i + 1] + 54 * bgra[i + 2]) >> 8; // Rec.709 xấp xỉ, như dò viền
                    n++;
                }
            }
            sig[r * Cols + c] = n == 0 ? (byte)0 : (byte)(sum / n);
        }
        return sig;
    }

    public const int RegionCols = 8;
    public const int RegionRows = 2;

    /// <summary>
    /// B3: chữ ký của RIÊNG vùng chữ (lưới 8×2 trung bình độ xám) — dùng để biết chữ có thật sự đổi hay chỉ nền quanh nó đang động.
    /// </summary>
    public static byte[] RegionSignature(ReadOnlySpan<byte> bgra, int width, int height, int stride, Box rectPx)
    {
        var sig = new byte[RegionCols * RegionRows];
        int x0 = Math.Clamp((int)Math.Floor(rectPx.X), 0, width), y0 = Math.Clamp((int)Math.Floor(rectPx.Y), 0, height);
        int x1 = Math.Clamp((int)Math.Ceiling(rectPx.X + rectPx.W), 0, width), y1 = Math.Clamp((int)Math.Ceiling(rectPx.Y + rectPx.H), 0, height);
        if (x1 <= x0 || y1 <= y0) return sig;
        for (int r = 0; r < RegionRows; r++)
        for (int c = 0; c < RegionCols; c++)
        {
            int cx0 = x0 + c * (x1 - x0) / RegionCols, cx1 = x0 + (c + 1) * (x1 - x0) / RegionCols;
            int cy0 = y0 + r * (y1 - y0) / RegionRows, cy1 = y0 + (r + 1) * (y1 - y0) / RegionRows;
            long sum = 0; int n = 0;
            for (int y = cy0; y < Math.Max(cy1, cy0 + 1) && y < height; y++)
                for (int x = cx0; x < Math.Max(cx1, cx0 + 1) && x < width; x++)
                {
                    int i = y * stride + x * 4;
                    sum += (18 * bgra[i] + 183 * bgra[i + 1] + 54 * bgra[i + 2]) >> 8;
                    n++;
                }
            sig[r * RegionCols + c] = n == 0 ? (byte)0 : (byte)(sum / n);
        }
        return sig;
    }

    /// <summary>Chỉ số ô (hàng*16+cột) mà box chuẩn hoá 0..1 chạm tới.</summary>
    public static IReadOnlyList<int> CellsOf(Box box01)
    {
        const double eps = 1e-9;
        int c0 = Math.Clamp((int)Math.Floor(box01.X * Cols), 0, Cols - 1);
        int c1 = Math.Clamp((int)Math.Floor((box01.X + box01.W) * Cols - eps), 0, Cols - 1);
        int r0 = Math.Clamp((int)Math.Floor(box01.Y * Rows), 0, Rows - 1);
        int r1 = Math.Clamp((int)Math.Floor((box01.Y + box01.H) * Rows - eps), 0, Rows - 1);
        var cells = new List<int>();
        for (int r = r0; r <= Math.Max(r0, r1); r++)
            for (int c = c0; c <= Math.Max(c0, c1); c++)
                cells.Add(r * Cols + c);
        return cells;
    }
}

public enum MotionState { Moving, Stable }

public sealed record MotionUpdate(
    MotionState State, IReadOnlyList<int> ChangedCells, bool BecameStable, bool BecameMoving, bool MatchMode);

/// <summary>
/// Máy trạng thái Động/Đứng yên + ô nền động (≥ BackgroundCellMs liên tục) + chế độ trận (N3, N4).
/// Thời gian do người gọi cấp (nowMs) ⇒ test bằng dãy chữ ký giả lập.
/// </summary>
public sealed class MotionTracker(TuningConfig t)
{
    private byte[]? _prev;
    private readonly long[] _lastChange = Enumerable.Repeat(long.MinValue / 2, FrameSignature.Cells).ToArray();
    private readonly long[] _runStart = Enumerable.Repeat(-1L, FrameSignature.Cells).ToArray();
    private readonly HashSet<int> _bg = new();
    private readonly HashSet<int> _dirty = new();
    private MotionState _state = MotionState.Moving;
    private long _lastMotionMs;
    private bool _first = true;
    private bool _match;
    private readonly Queue<long> _beats = new();

    public IReadOnlySet<int> BackgroundCells => _bg;

    /// <summary>Ô đổi (không tính ô nền) kể từ lần <see cref="Ack"/> gần nhất.</summary>
    public IReadOnlySet<int> DirtySinceAck => _dirty;

    public void Ack() => _dirty.Clear();

    /// <param name="protectedCells">ô của item đang vẽ.</param>
    /// <param name="textChangedCells">ô của item đang vẽ mà CHÍNH vùng chữ đã đổi (B3). null ⇒ không phân biệt (hành vi cũ).</param>
    public MotionUpdate Update(long nowMs, byte[] signature, IReadOnlySet<int>? protectedCells = null, IReadOnlySet<int>? textChangedCells = null)
    {
        var effective = new List<int>();
        if (_prev is not null)
        {
            for (int c = 0; c < FrameSignature.Cells; c++)
            {
                bool changed = Math.Abs(signature[c] - _prev[c]) > t.CellDiffThreshold;
                if (changed)
                {
                    if (_runStart[c] < 0) _runStart[c] = nowMs;
                    _lastChange[c] = nowMs;
                    if (!_bg.Contains(c) && nowMs - _runStart[c] >= t.BackgroundCellMs) _bg.Add(c);
                    // Ô nền động chỉ tính là "đổi" khi có item đang vẽ trong ô VÀ chữ của item đó thật sự đổi (B3):
                    // nền chạy quanh chữ đứng yên không được gỡ item ⇒ không nhấp nháy.
                    bool protectedBg = _bg.Contains(c) && (protectedCells?.Contains(c) ?? false)
                                       && (textChangedCells is null || textChangedCells.Contains(c));
                    if (!_bg.Contains(c) || protectedBg) effective.Add(c);
                }
                else
                {
                    _runStart[c] = -1;
                    if (_bg.Contains(c) && nowMs - _lastChange[c] >= t.BackgroundCellMs) _bg.Remove(c);
                }
            }
        }
        _prev = signature;

        if (_first) { _first = false; _lastMotionMs = nowMs; }

        bool becameMoving = false, becameStable = false;
        if (effective.Count > 0)
        {
            foreach (var c in effective) _dirty.Add(c);
            _lastMotionMs = nowMs;
            if (_state == MotionState.Stable) becameMoving = true;
            _state = MotionState.Moving;

            // Chế độ trận: trong cửa sổ gần nhất, > ratio các ô không-nền đã đổi.
            int considered = FrameSignature.Cells - _bg.Count;
            int recent = 0;
            for (int c = 0; c < FrameSignature.Cells; c++)
                if (!_bg.Contains(c) && nowMs - _lastChange[c] <= t.MatchModeWindowMs) recent++;
            // NB7: chế độ trận cần chuyển động KÉO DÀI — ≥ MatchModeMinBeats nhịp vượt ngưỡng trong MatchModeSustainMs;
            // một lần đổi trang toàn màn (menu → menu) chỉ là 1 nhịp nên không tính.
            if (considered > 0 && recent > t.MatchModeCellRatio * considered) _beats.Enqueue(nowMs);
            while (_beats.Count > 0 && nowMs - _beats.Peek() > t.MatchModeSustainMs) _beats.Dequeue();
            if (_beats.Count >= t.MatchModeMinBeats) _match = true;
        }
        else if (_state == MotionState.Moving)
        {
            long calm = nowMs - _lastMotionMs;
            long needed = _match ? t.MatchModeCalmMs : t.StableMs;
            if (calm >= needed)
            {
                _state = MotionState.Stable;
                becameStable = true;
                _match = false;
                _beats.Clear();
            }
        }
        return new MotionUpdate(_state, effective, becameStable, becameMoving, _match);
    }
}
