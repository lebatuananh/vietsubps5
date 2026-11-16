namespace Engine.Ocr;

/// <summary>Đoạn golden không khớp + dòng OCR gần nhất (để người đọc bằng mắt).</summary>
public sealed record UnmatchedSegment(string Golden, string? NearestOcrLine);

/// <param name="SegmentRate">(a) tỉ lệ đoạn khớp 0..1; null khi golden rỗng (không xác định).</param>
/// <param name="WordRate">(b) tỉ lệ từ đúng theo thứ tự 0..1; null khi golden rỗng.</param>
public sealed record ScoreResult(
    int Segments,
    int SegmentsMatched,
    double? SegmentRate,
    int Words,
    int WordsMatched,
    double? WordRate,
    IReadOnlyList<UnmatchedSegment> Unmatched)
{
    public bool Undefined => Segments == 0;
}

/// <summary>Chấm OCR so với golden (ground truth chép tay) — hàm thuần, không I/O.</summary>
public static class OcrScorer
{
    /// <summary>Số dòng OCR liền nhau tối đa được ghép khi so khớp một đoạn golden.</summary>
    public const int MaxJoinedLines = 3;

    public static ScoreResult Score(IReadOnlyList<string> goldenSegments, IReadOnlyList<string> ocrLines)
    {
        var golden = goldenSegments.Select(NormalizeText).Where(s => s.Length > 0).ToList();
        var lines = ocrLines.Select(NormalizeText).Where(s => s.Length > 0).ToList();

        if (golden.Count == 0)
            return new ScoreResult(0, 0, null, 0, 0, null, Array.Empty<UnmatchedSegment>());

        // (a) đoạn khớp theo BỘI SỐ: đoạn golden xuất hiện k lần cần ≥ k lần xuất hiện riêng biệt
        // (không chồng lấn) trong văn bản OCR; mỗi lần xuất hiện theo biên từ và trải trên ≤ MaxJoinedLines dòng liền nhau.
        int matched = 0;
        var unmatched = new List<UnmatchedSegment>();
        foreach (var group in golden.GroupBy(g => g, StringComparer.Ordinal))
        {
            int need = group.Count();
            int found = CountDistinctOccurrences(lines, group.Key);
            int ok = Math.Min(need, found);
            matched += ok;
            for (int i = ok; i < need; i++)
                unmatched.Add(new UnmatchedSegment(group.Key, NearestLine(group.Key, lines)));
        }

        // (b) độ phủ từ theo ĐA TẬP HỢP (không xét thứ tự): mỗi từ golden khớp tối đa một lần với một từ OCR.
        var goldenWords = golden.SelectMany(SplitWords).ToList();
        var ocrCounts = lines.SelectMany(SplitWords).GroupBy(w => w, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        int wordsMatched = 0;
        foreach (var w in goldenWords)
            if (ocrCounts.TryGetValue(w, out var left) && left > 0) { wordsMatched++; ocrCounts[w] = left - 1; }

        return new ScoreResult(
            golden.Count, matched, (double)matched / golden.Count,
            goldenWords.Count, wordsMatched, goldenWords.Count == 0 ? null : (double)wordsMatched / goldenWords.Count,
            unmatched);
    }

    /// <summary>Gộp mọi khoảng trắng liên tiếp thành 1 dấu cách, cắt hai đầu. Giữ hoa/thường, dấu câu.</summary>
    internal static string NormalizeText(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        bool pendingSpace = false;
        foreach (char c in s)
        {
            if (char.IsWhiteSpace(c)) { pendingSpace = sb.Length > 0; continue; }
            if (pendingSpace) { sb.Append(' '); pendingSpace = false; }
            sb.Append(c);
        }
        return sb.ToString();
    }

    private static string[] SplitWords(string s) => s.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Số lần xuất hiện riêng biệt (không chồng lấn, quét từ trái) của <paramref name="needle"/> trong văn bản
    /// OCR = các dòng nối bằng 1 dấu cách; chỉ tính lần theo biên từ và chạm ≤ MaxJoinedLines dòng.
    /// </summary>
    internal static int CountDistinctOccurrences(IReadOnlyList<string> lines, string needle)
    {
        var sb = new System.Text.StringBuilder();
        var starts = new List<int>(); var ends = new List<int>(); // [start,end) của từng dòng trong văn bản nối
        foreach (var line in lines)
        {
            if (sb.Length > 0) sb.Append(' ');
            starts.Add(sb.Length); sb.Append(line); ends.Add(sb.Length);
        }
        string text = sb.ToString();

        int count = 0, from = 0;
        while (from <= text.Length - needle.Length)
        {
            int i = text.IndexOf(needle, from, StringComparison.Ordinal);
            if (i < 0) break;
            int end = i + needle.Length;
            bool startOk = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
            bool endOk = end == text.Length || !char.IsLetterOrDigit(text[end]);
            if (startOk && endOk && LinesTouched(starts, ends, i, end) <= MaxJoinedLines)
            {
                count++;
                from = end; // không chồng lấn
            }
            else from = i + 1;
        }
        return count;
    }

    private static int LinesTouched(List<int> starts, List<int> ends, int begin, int end)
    {
        int n = 0;
        for (int k = 0; k < starts.Count; k++)
            if (starts[k] < end && ends[k] > begin) n++;
        return n;
    }

    /// <summary>Dòng OCR có chuỗi con gần golden nhất (khoảng cách chỉnh sửa xấp xỉ chuỗi con — Sellers).</summary>
    private static string? NearestLine(string golden, IReadOnlyList<string> lines)
    {
        string? best = null;
        int bestDist = int.MaxValue;
        foreach (var line in lines)
        {
            int d = ApproxSubstringDistance(golden, line);
            if (d < bestDist) { bestDist = d; best = line; }
        }
        return best;
    }

    private static int ApproxSubstringDistance(string pattern, string text)
    {
        var prev = new int[pattern.Length + 1];
        var cur = new int[pattern.Length + 1];
        for (int i = 0; i <= pattern.Length; i++) prev[i] = i;
        int best = prev[pattern.Length];
        foreach (char t in text)
        {
            cur[0] = 0; // bắt đầu khớp ở bất kỳ vị trí nào của text
            for (int i = 1; i <= pattern.Length; i++)
            {
                int cost = pattern[i - 1] == t ? 0 : 1;
                cur[i] = Math.Min(Math.Min(cur[i - 1] + 1, prev[i] + 1), prev[i - 1] + cost);
            }
            best = Math.Min(best, cur[pattern.Length]);
            (prev, cur) = (cur, prev);
        }
        return best;
    }
}
