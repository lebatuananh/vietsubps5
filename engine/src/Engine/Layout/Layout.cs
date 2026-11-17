using System.Security.Cryptography;
using System.Text;
using Engine.Config;
using Engine.Geometry;
using Engine.Pipeline;

namespace Engine.Layout;

public sealed record OcrWordData(string Text, Box BoxPx);

public sealed record OcrLineData(string Text, Box BoxPx, IReadOnlyList<OcrWordData> Words);

/// <summary>Một đoạn chữ cần dịch: sau tách dòng gộp cột, gộp đoạn văn, nhóm cột/hàng.</summary>
public sealed record LayoutBlock(
    string Text, Box BoxPx, Box Box01, int LineCount, double LineHeightPx, string Group, int FontPx, int MaxLines);

public static class LayoutBuilder
{
    private static readonly HashSet<string> IconTokens = new(StringComparer.OrdinalIgnoreCase) { "R1", "L1", "R2", "L2", "R3", "L3" };

    private sealed class Seg
    {
        public string Text = "";
        public double X, Y, W, H;
        public int Lines = 1;
        public double HeightSum;
        public double LastY, LastH;
    }

    /// <param name="content">contentRect (px) dùng chuẩn hoá toạ độ; chiều cao của nó quy ra khung 1080.</param>
    public static IReadOnlyList<LayoutBlock> Build(
        IReadOnlyList<OcrLineData> lines, ContentRect content, TuningConfig t, double fontScale)
    {
        // 0. Tách dòng OCR thành đoạn khi khe giữa hai từ liền nhau > SplitGapFactor × cao dòng (hàng nhiều cột bị gộp).
        var segs = new List<Seg>();
        foreach (var line in lines)
        {
            var words = line.Words.OrderBy(w => w.BoxPx.X).ToList();
            if (words.Count == 0) continue;
            double lh = line.BoxPx.H;
            var cur = new List<OcrWordData> { words[0] };
            for (int i = 1; i < words.Count; i++)
            {
                double gap = words[i].BoxPx.X - (words[i - 1].BoxPx.X + words[i - 1].BoxPx.W);
                if (gap > t.SplitGapFactor * lh) { segs.Add(Make(cur)); cur = new List<OcrWordData>(); }
                cur.Add(words[i]);
            }
            segs.Add(Make(cur));
        }

        // 1. Bỏ nhiễu: không có chữ, ≤ 1 chữ cái, token icon tay cầm.
        segs = segs.Where(s => Keep(s.Text)).ToList();

        // 2. Gộp đoạn văn: dòng liên tiếp cùng mép trái, cao tương đương, khe dọc nhỏ.
        double leftTol = t.MergeLeftTolerance * content.Width;
        var blocks = new List<Seg>();
        foreach (var s in segs.OrderBy(s => s.Y).ThenBy(s => s.X))
        {
            var target = blocks.FirstOrDefault(b =>
                Math.Abs(b.X - s.X) <= leftTol
                && Math.Abs(b.LastH - s.H) <= t.MergeHeightTolerance * Math.Max(b.LastH, s.H)
                && s.Y - (b.LastY + b.LastH) <= t.MergeGapFactor * s.H
                && s.Y - (b.LastY + b.LastH) >= -0.5 * s.H);
            if (target is null) { blocks.Add(s); continue; }
            target.Text += " " + s.Text;
            double x2 = Math.Max(target.X + target.W, s.X + s.W);
            target.X = Math.Min(target.X, s.X);
            target.W = x2 - target.X;
            target.H = s.Y + s.H - target.Y;
            target.Lines++;
            target.HeightSum += s.H;
            target.LastY = s.Y; target.LastH = s.H;
        }

        // 3. Nhóm: cột (cùng mép trái + cùng cao dòng, ≥ 2 thành viên) → hàng ngang (≥ 2 nhãn sát nhau) → solo.
        var group = new string?[blocks.Count];
        var colReps = new List<(double left, double h, List<int> members)>();
        for (int i = 0; i < blocks.Count; i++)
        {
            double avgH = blocks[i].HeightSum / blocks[i].Lines;
            var rep = colReps.FirstOrDefault(r => Math.Abs(r.left - blocks[i].X) <= leftTol
                                                 && Math.Abs(r.h - avgH) <= t.GroupHeightTolerance * Math.Max(r.h, avgH));
            if (rep.members is null) colReps.Add((blocks[i].X, avgH, new List<int> { i })); else rep.members.Add(i);
        }
        int col = 0;
        foreach (var r in colReps.Where(r => r.members.Count >= 2))
        {
            col++;
            foreach (var i in r.members) group[i] = $"col-{col}";
        }
        int row = 0;
        var rest = Enumerable.Range(0, blocks.Count).Where(i => group[i] is null).OrderBy(i => blocks[i].Y + blocks[i].H / 2).ToList();
        var used = new HashSet<int>();
        foreach (var i in rest)
        {
            if (used.Contains(i)) continue;
            double cy = blocks[i].Y + blocks[i].H / 2, h = blocks[i].H;
            var mates = rest.Where(j => !used.Contains(j)
                    && Math.Abs(blocks[j].Y + blocks[j].H / 2 - cy) <= t.RowCenterTolerance * content.Height + 0.15 * h
                    && Math.Abs(blocks[j].H - h) <= t.GroupHeightTolerance * Math.Max(blocks[j].H, h))
                .OrderBy(j => blocks[j].X).ToList();
            // chỉ giữ các nhãn nối tiếp nhau với khe ≤ RowLabelGapMax × bề rộng
            var chain = new List<int>();
            foreach (var j in mates)
            {
                if (chain.Count == 0) { chain.Add(j); continue; }
                var prev = blocks[chain[^1]];
                if (blocks[j].X - (prev.X + prev.W) <= t.RowLabelGapMax * content.Width) chain.Add(j);
                else if (chain.Count < 2) { chain.Clear(); chain.Add(j); }
                else break;
            }
            if (chain.Count >= 2 && chain.Contains(i))
            {
                row++;
                foreach (var j in chain) { group[j] = $"row-{row}"; used.Add(j); }
            }
        }
        int solo = 0;
        for (int i = 0; i < blocks.Count; i++) group[i] ??= $"solo-{++solo}";

        // 4. Hoàn thiện LayoutBlock (toạ độ chuẩn hoá theo contentRect; cỡ quy về khung cao 1080).
        double toPx1080 = 1080.0 / content.Height;
        var result = new List<LayoutBlock>(blocks.Count);
        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i];
            var box = new Box(b.X, b.Y, b.W, b.H);
            double lineH = b.HeightSum / b.Lines;
            int fontPx = Math.Max(1, (int)Math.Round(lineH * toPx1080 * fontScale));
            result.Add(new LayoutBlock(b.Text, box, Normalizer.Normalize(box, content), b.Lines, lineH, group[i]!, fontPx, b.Lines == 1 ? 1 : Math.Min(6, b.Lines + 2))); // Rev 1.4: nhãn một dòng = 1; đoạn văn = số dòng gốc + 2
        }
        // Cùng nhóm ⇒ cùng cỡ gốc (trung vị): chiều cao hộp OCR dao động theo nét lên/xuống của chữ (descender/ dấu),
        // không phản ánh cỡ chữ thật — nếu không, các ô cùng cột lệch cỡ trên TV (X4).
        foreach (var g in result.Where(r => !r.Group.StartsWith("solo-")).GroupBy(r => r.Group).ToList())
        {
            var sizes = g.Select(r => r.FontPx).OrderBy(v => v).ToList();
            int median = sizes[sizes.Count / 2];
            for (int i = 0; i < result.Count; i++)
                if (result[i].Group == g.Key) result[i] = result[i] with { FontPx = median };
        }
        return result.OrderBy(r => r.BoxPx.Y).ThenBy(r => r.BoxPx.X).ToList();
    }

    private static Seg Make(List<OcrWordData> ws)
    {
        double x1 = ws.Min(w => w.BoxPx.X), y1 = ws.Min(w => w.BoxPx.Y);
        double x2 = ws.Max(w => w.BoxPx.X + w.BoxPx.W), y2 = ws.Max(w => w.BoxPx.Y + w.BoxPx.H);
        return new Seg
        {
            Text = string.Join(' ', ws.Select(w => w.Text)),
            X = x1, Y = y1, W = x2 - x1, H = y2 - y1,
            HeightSum = y2 - y1, LastY = y1, LastH = y2 - y1,
        };
    }

    private static bool Keep(string text)
    {
        var s = text.Trim();
        if (s.Length == 0 || IconTokens.Contains(s)) return false;
        return s.Count(char.IsLetter) >= 2;
    }
}

public static class ItemId
{
    /// <summary>
    /// Băm(ô lưới 16×9 chứa góc trên-trái + câu gốc chuẩn hoá + số thứ tự lần xuất hiện của cặp (ô, câu) theo thứ tự đọc), 12 ký tự hex.
    /// <paramref name="occurrence"/> làm <c>id</c> DUY NHẤT trong một gói (B1 review 3a) mà vẫn ổn định giữa các khung.
    /// </summary>
    public static string Compute(Box box01, string normalizedSource, int occurrence = 0)
    {
        int cx = Math.Clamp((int)Math.Floor(box01.X * FrameSignature.Cols), 0, FrameSignature.Cols - 1);
        int cy = Math.Clamp((int)Math.Floor(box01.Y * FrameSignature.Rows), 0, FrameSignature.Rows - 1);
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(occurrence == 0 ? $"{cx},{cy}|{normalizedSource}" : $"{cx},{cy}|{normalizedSource}|#{occurrence}"));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }
}

public readonly record struct SampledColors(uint Cover, uint Text, double RimSpread);

public static class ColorSampler
{
    /// <summary>Cover = trung vị màu vành <paramref name="rim"/> px quanh bbox; Text = trung vị 10% điểm xa nền nhất trong bbox.</summary>
    public static SampledColors Sample(ReadOnlySpan<byte> bgra, int width, int height, int stride, Box rectPx, int rim = 3)
    {
        int x0 = Math.Clamp((int)Math.Floor(rectPx.X), 0, width), y0 = Math.Clamp((int)Math.Floor(rectPx.Y), 0, height);
        int x1 = Math.Clamp((int)Math.Ceiling(rectPx.X + rectPx.W), 0, width), y1 = Math.Clamp((int)Math.Ceiling(rectPx.Y + rectPx.H), 0, height);

        var rimPx = new List<(byte r, byte g, byte b)>();
        int rx0 = Math.Max(0, x0 - rim), ry0 = Math.Max(0, y0 - rim), rx1 = Math.Min(width, x1 + rim), ry1 = Math.Min(height, y1 + rim);
        for (int y = ry0; y < ry1; y++)
            for (int x = rx0; x < rx1; x++)
            {
                if (x >= x0 && x < x1 && y >= y0 && y < y1) continue; // trong bbox
                rimPx.Add(At(bgra, stride, x, y));
            }

        var inner = new List<(byte r, byte g, byte b)>();
        for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) inner.Add(At(bgra, stride, x, y));
        if (rimPx.Count == 0) rimPx = inner;
        if (rimPx.Count == 0) return new SampledColors(0xFF000000, 0xFFFFFFFF, 0);

        var cover = Median(rimPx);
        double spread = rimPx.Average(p => (Math.Abs(p.r - cover.r) + Math.Abs(p.g - cover.g) + Math.Abs(p.b - cover.b)) / 3.0);

        var text = cover;
        if (inner.Count > 0)
        {
            int take = Math.Max(1, inner.Count / 10);
            var far = inner.OrderByDescending(p => Math.Abs(p.r - cover.r) + Math.Abs(p.g - cover.g) + Math.Abs(p.b - cover.b)).Take(take).ToList();
            text = Median(far);
        }
        return new SampledColors(Pack(cover), Pack(text), spread);
    }

    /// <summary>configured = auto|solid|card|none. auto: spread &gt; threshold ⇒ card, ngược lại solid (N8).</summary>
    public static string DecideCoverMode(string configured, double rimSpread, double threshold)
        => configured.Equals("auto", StringComparison.OrdinalIgnoreCase) ? (rimSpread > threshold ? "card" : "solid") : configured.ToLowerInvariant();

    public static string Hex(uint argb) => $"#{argb:X8}";

    private static (byte r, byte g, byte b) At(ReadOnlySpan<byte> p, int stride, int x, int y)
    {
        int i = y * stride + x * 4;
        return (p[i + 2], p[i + 1], p[i]);
    }

    private static (byte r, byte g, byte b) Median(List<(byte r, byte g, byte b)> px)
    {
        byte M(Func<(byte r, byte g, byte b), byte> f) { var a = px.Select(f).OrderBy(v => v).ToArray(); return a[a.Length / 2]; }
        return (M(p => p.r), M(p => p.g), M(p => p.b));
    }

    private static uint Pack((byte r, byte g, byte b) c) => 0xFF000000u | ((uint)c.r << 16) | ((uint)c.g << 8) | c.b;
}
