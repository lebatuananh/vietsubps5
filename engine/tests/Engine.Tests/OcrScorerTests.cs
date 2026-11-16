using Engine.Ocr;

namespace Engine.Tests;

public class OcrScorerTests
{
    private static ScoreResult Score(string[] golden, params string[] ocr) => OcrScorer.Score(golden, ocr);

    [Fact]
    public void MergedRow_AllThreeSegmentsMatch_AndAllFiveWords()
    {
        var r = Score(new[] { "Board", "Season Objectives Update", "Today" },
            "Board Season Objectives Update Today");
        Assert.Equal(3, r.Segments);
        Assert.Equal(3, r.SegmentsMatched);
        Assert.Equal(1.0, r.SegmentRate);
        Assert.Equal(5, r.Words);
        Assert.Equal(5, r.WordsMatched);
        Assert.Equal(1.0, r.WordRate);
    }

    [Fact]
    public void MatchMustEndOnWordBoundary_BoardVsBoardroom_NoMatch()
    {
        var r = Score(new[] { "Board" }, "Boardroom meeting");
        Assert.Equal(0, r.SegmentsMatched);
    }

    [Fact]
    public void MatchMustStartOnWordBoundary_RoomVsBoardroom_NoMatch()
    {
        var r = Score(new[] { "room" }, "Boardroom meeting");
        Assert.Equal(0, r.SegmentsMatched);
    }

    [Fact]
    public void PunctuationCountsAsBoundary_TodayMatchesInsideTodayComma()
    {
        var r = Score(new[] { "Today" }, "Board Today, Scout");
        Assert.Equal(1, r.SegmentsMatched);
    }

    [Fact]
    public void SegmentSplitOverTwoConsecutiveLines_Matches()
    {
        var r = Score(new[] { "Your contract offer has been accepted by the player" },
            "Your contract offer has been", "accepted by the player");
        Assert.Equal(1, r.Segments);
        Assert.Equal(1, r.SegmentsMatched);
    }

    [Fact]
    public void OneWrongCharacter_SegmentFails_WordsTwoOfThree_ListedWithNearestLine()
    {
        var r = Score(new[] { "Transfer Budget: €45.0M" }, "Squad Hub", "Transfer Budget: €45.OM");
        Assert.Equal(0, r.SegmentsMatched);
        Assert.Equal(0.0, r.SegmentRate);
        Assert.Equal(3, r.Words);
        Assert.Equal(2, r.WordsMatched);
        var u = Assert.Single(r.Unmatched);
        Assert.Equal("Transfer Budget: €45.0M", u.Golden);
        Assert.Equal("Transfer Budget: €45.OM", u.NearestOcrLine);
    }

    [Fact]
    public void ExtraWhitespace_IsNormalizedBeforeComparing()
    {
        var r = Score(new[] { "Scout Report" }, "Scout   Report ");
        Assert.Equal(1, r.SegmentsMatched);
    }

    [Fact]
    public void CaseIsKept_InboxVsInbox_NoMatch()
    {
        var r = Score(new[] { "INBOX" }, "Inbox");
        Assert.Equal(0, r.SegmentsMatched);
    }

    [Fact]
    public void DoesNotJoinMoreThanThreeLines()
    {
        var r = Score(new[] { "one two three four" }, "one", "two", "three", "four");
        Assert.Equal(0, r.SegmentsMatched);
        // ... nhưng 3 dòng thì được.
        var r3 = Score(new[] { "one two three" }, "one", "two", "three");
        Assert.Equal(1, r3.SegmentsMatched);
    }

    [Fact]
    public void DuplicateGoldenSegment_SecondOccurrenceMisread_OnlyOneOfTwoMatches()
    {
        // NB3: "Board" ×2 trong golden, OCR đọc lần hai thành "B0ard" ⇒ 1/2 (không để một dòng che cả hai).
        var r = Score(new[] { "Board", "Board" }, "Board", "B0ard");
        Assert.Equal(2, r.Segments);
        Assert.Equal(1, r.SegmentsMatched);
        Assert.Equal(0.5, r.SegmentRate);
        Assert.Single(r.Unmatched);
    }

    [Fact]
    public void DuplicateGoldenSegment_TwoDistinctOccurrences_BothMatch()
    {
        Assert.Equal(2, Score(new[] { "Board", "Board" }, "Board", "Board").SegmentsMatched);
        // Hai lần xuất hiện riêng biệt trong CÙNG một dòng (hàng bị gộp) vẫn tính 2.
        Assert.Equal(2, Score(new[] { "Board", "Board" }, "Board x Board").SegmentsMatched);
        // Một lần xuất hiện duy nhất không đủ cho hai đoạn golden.
        Assert.Equal(1, Score(new[] { "Board", "Board" }, "Board only").SegmentsMatched);
    }

    [Fact]
    public void WordCoverage_IsOrderIndependent_ColumnMajorOcrVsRowMajorGolden()
    {
        // NB6: golden theo hàng, OCR trả theo cột ⇒ (b) vẫn 100%.
        var golden = new[] { "Board", "Season Update", "Today", "Committee", "Contract", "Yesterday" };
        var r = Score(golden, "Board", "Committee", "Season Update", "Contract", "Today", "Yesterday");
        Assert.Equal(7, r.Words);
        Assert.Equal(7, r.WordsMatched);
        Assert.Equal(1.0, r.WordRate);
        Assert.Equal(6, r.SegmentsMatched);
    }

    [Fact]
    public void WordCoverage_IsMultiset_EachOcrWordUsedAtMostOnce()
    {
        var r = Score(new[] { "Nov", "Nov" }, "Nov");
        Assert.Equal(2, r.Words);
        Assert.Equal(1, r.WordsMatched);
    }

    [Fact]
    public void EmptyGolden_IsUndefined_NotDivideByZero()
    {
        var r = Score(Array.Empty<string>(), "anything");
        Assert.True(r.Undefined);
        Assert.Null(r.SegmentRate);
        Assert.Null(r.WordRate);
        Assert.Equal(0, r.Segments);
    }
}
