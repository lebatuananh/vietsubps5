using Engine.Config;
using Engine.Geometry;
using Engine.Pipeline;

namespace Engine.Tests;

public class FrameSignatureTests
{
    private static byte[] Img(int w, int h, byte gray)
    {
        var px = new byte[w * h * 4];
        for (int i = 0; i < px.Length; i += 4) { px[i] = gray; px[i + 1] = gray; px[i + 2] = gray; px[i + 3] = 255; }
        return px;
    }

    [Fact]
    public void Uniform_AllCellsEqualToGray()
    {
        var sig = FrameSignature.Compute(Img(1920, 1080, 100), 1920, 1080, 1920 * 4, new ContentRect(0, 0, 1920, 1080));
        Assert.Equal(144, sig.Length);
        Assert.All(sig, b => Assert.InRange(b, 99, 101));
    }

    [Fact]
    public void ChangingRegion_ChangesOnlyIntersectingCells()
    {
        var a = Img(1920, 1080, 20);
        var b = Img(1920, 1080, 20);
        // vùng sáng trong ô cột 3, hàng 2 (mỗi ô 120×120)
        for (int y = 240; y < 360; y++) for (int x = 360; x < 480; x++) { int i = (y * 1920 + x) * 4; b[i] = b[i + 1] = b[i + 2] = 220; }
        var r = new ContentRect(0, 0, 1920, 1080);
        var sa = FrameSignature.Compute(a, 1920, 1080, 1920 * 4, r);
        var sb = FrameSignature.Compute(b, 1920, 1080, 1920 * 4, r);
        var diff = Enumerable.Range(0, 144).Where(i => Math.Abs(sa[i] - sb[i]) > 6).ToList();
        Assert.Equal(new[] { 2 * 16 + 3 }, diff);
    }

    [Fact]
    public void ContentRect_OffsetIsRespected()
    {
        var img = Img(2000, 1080, 0);
        for (int y = 0; y < 1080; y++) for (int x = 40; x < 1960; x++) { int i = (y * 2000 + x) * 4; img[i] = img[i + 1] = img[i + 2] = 150; }
        var sig = FrameSignature.Compute(img, 2000, 1080, 2000 * 4, new ContentRect(40, 0, 1920, 1080));
        Assert.All(sig, b => Assert.InRange(b, 149, 151)); // viền đen ngoài contentRect không lọt vào
    }

    [Fact]
    public void CellsOf_BoxCoversExpectedCells()
    {
        // box chuẩn hoá nằm gọn trong ô (0,0)
        Assert.Equal(new[] { 0 }, FrameSignature.CellsOf(new Box(0.01, 0.01, 0.02, 0.02)));
        // box trải 2 cột × 2 hàng quanh ô (1,1)/(2,2)
        var cells = FrameSignature.CellsOf(new Box(0.0626, 0.1112, 0.0625, 0.1112)).OrderBy(c => c).ToArray();
        Assert.Equal(new[] { 1 * 16 + 1, 1 * 16 + 2, 2 * 16 + 1, 2 * 16 + 2 }, cells);
    }
}

public class MotionTrackerTests
{
    private static TuningConfig T() => new() { StableMs = 250, BackgroundCellMs = 3000, MatchModeCellRatio = 0.5, MatchModeWindowMs = 3000, MatchModeCalmMs = 1500, CellDiffThreshold = 6 };
    private static byte[] Sig(byte v = 50) { var s = new byte[144]; Array.Fill(s, v); return s; }

    [Fact]
    public void Identical_BecomesStableOnce_AfterStableMs()
    {
        var m = new MotionTracker(T());
        var s = Sig();
        Assert.Equal(MotionState.Moving, m.Update(0, s).State);
        Assert.Equal(MotionState.Moving, m.Update(200, s).State);
        var u = m.Update(260, s);
        Assert.Equal(MotionState.Stable, u.State);
        Assert.True(u.BecameStable);
        Assert.False(m.Update(500, s).BecameStable); // chỉ một lần
    }

    [Fact]
    public void Change_AfterStable_BecomesMoving_WithChangedCells()
    {
        var m = new MotionTracker(T());
        var s = Sig();
        m.Update(0, s); m.Update(300, s);
        var moved = Sig(); moved[20] = 200; moved[21] = 200;
        var u = m.Update(550, moved);
        Assert.True(u.BecameMoving);
        Assert.Equal(MotionState.Moving, u.State);
        Assert.Equal(new[] { 20, 21 }, u.ChangedCells.OrderBy(c => c).ToArray());
    }

    [Fact]
    public void SmallDiff_BelowThreshold_IsNotMotion()
    {
        var m = new MotionTracker(T());
        var s = Sig(50);
        m.Update(0, s); m.Update(300, s);
        var noisy = Sig(50); noisy[5] = 54; // |Δ| = 4 < 6
        Assert.Empty(m.Update(550, noisy).ChangedCells);
    }

    [Fact]
    public void ContinuouslyChangingCell_BecomesBackground_AndStableIsReachedDespiteIt()
    {
        var m = new MotionTracker(T());
        long now = 0;
        byte flip = 50;
        MotionUpdate last = default!;
        for (; now <= 3600; now += 250)
        {
            var s = Sig(); flip = (byte)(flip == 50 ? 120 : 50); s[7] = flip; // ô 7 luôn đổi
            last = m.Update(now, s);
        }
        Assert.Contains(7, m.BackgroundCells);
        Assert.Equal(MotionState.Stable, last.State);
    }

    [Fact]
    public void ProtectedCell_IsNotIgnoredEvenIfBackground()
    {
        var m = new MotionTracker(T());
        long now = 0; byte flip = 50; MotionUpdate last = default!;
        var prot = new HashSet<int> { 7 };
        for (; now <= 3600; now += 250)
        {
            var s = Sig(); flip = (byte)(flip == 50 ? 120 : 50); s[7] = flip;
            last = m.Update(now, s, prot);
        }
        Assert.Equal(MotionState.Moving, last.State); // ô 7 chứa item đang vẽ ⇒ vẫn tính
        Assert.Contains(7, last.ChangedCells);
    }

    [Fact]
    public void MatchMode_WhenMostCellsChange_StaysMovingUntilCalm()
    {
        var m = new MotionTracker(T());
        long now = 0; byte v = 30;
        MotionUpdate u = default!;
        for (int i = 0; i < 6; i++, now += 250) { v = (byte)(v == 30 ? 200 : 30); u = m.Update(now, Sig(v)); } // 100% ô đổi
        Assert.True(u.MatchMode);
        var still = Sig(v);
        u = m.Update(now, still); now += 300;                 // đứng yên 300 ms > StableMs nhưng < 1500
        u = m.Update(now, still);
        Assert.Equal(MotionState.Moving, u.State);
        Assert.True(u.MatchMode);
        u = m.Update(now + 1600, still);                      // đứng yên ≥ 1500 ms
        Assert.False(u.MatchMode);
        Assert.Equal(MotionState.Stable, u.State);
    }

    [Fact]
    public void DirtyCells_AccumulateUntilAck()
    {
        var m = new MotionTracker(T());
        m.Update(0, Sig());
        var a = Sig(); a[3] = 200; m.Update(250, a);
        var b = Sig(); b[3] = 200; b[9] = 200; m.Update(500, b);
        Assert.Equal(new[] { 3, 9 }, m.DirtySinceAck.OrderBy(c => c).ToArray());
        m.Ack();
        Assert.Empty(m.DirtySinceAck);
    }
}
