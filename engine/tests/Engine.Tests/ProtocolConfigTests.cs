using System.Text.Json;
using Engine.Config;
using Engine.Geometry;
using Engine.Protocol;

namespace Engine.Tests;

public class ProtocolTests
{
    private static OverlayItem Item(string? alt = "Ban LD", Box? room = null) => new(
        "4e1a9b27c0d3", new Box(0.052, 0.185, 0.048, 0.024), "Ban lãnh đạo", alt, 22, "col-1", 1, "start", false,
        "#FFFFFFFF", new CoverSpec("solid", "#FF14202C"), room);

    private static OverlayFrame Frame(params OverlayItem[] items) =>
        new(1, "sess-1", 7, 3000, new DisplaySpec(24, 0), items);

    [Fact]
    public void Frame_IsSingleLine_CamelCase_AndOmitsNulls()
    {
        var line = ProtocolJson.SerializeFrame(Frame(Item(alt: null)));
        Assert.DoesNotContain('\n', line);
        Assert.Contains("\"sessionId\":\"sess-1\"", line);
        Assert.Contains("\"textColor\":\"#FFFFFFFF\"", line);
        Assert.Contains("\"minFontPx\":24", line);
        Assert.DoesNotContain("\"alt\"", line);
        Assert.DoesNotContain("\"room\"", line);
        Assert.Contains("đ", line); // không escape tiếng Việt
    }

    [Fact]
    public void Frame_RoundTrips()
    {
        var f = Frame(Item(room: new Box(0.04, 0.18, 0.2, 0.04)));
        var back = ProtocolJson.ParseFrame(ProtocolJson.SerializeFrame(f))!;
        Assert.Equal(1, back.V);
        Assert.Equal("sess-1", back.SessionId);
        Assert.Equal(7, back.Seq);
        Assert.Equal(3000, back.TtlMs);
        Assert.Equal(24, back.Display!.MinFontPx);
        var it = Assert.Single(back.Items);
        Assert.Equal("Ban lãnh đạo", it.Text);
        Assert.Equal("Ban LD", it.Alt);
        Assert.Equal(0.052, it.Box.X, 9);
        Assert.Equal(0.2, it.Room!.Value.W, 9);
        Assert.Equal("solid", it.Cover.Mode);
    }

    [Fact]
    public void EmptyItems_SerializesAsEmptyArray()
    {
        var line = ProtocolJson.SerializeFrame(Frame());
        Assert.Contains("\"items\":[]", line);
        Assert.Empty(ProtocolJson.ParseFrame(line)!.Items);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"v\":1}")]
    public void ParseFrame_InvalidReturnsNull(string line) => Assert.Null(ProtocolJson.ParseFrame(line));

    [Fact]
    public void Hello_ExactWireFormat()
        => Assert.Equal("{\"hello\":{\"v\":1,\"token\":\"ACDEFHJK\"}}", ProtocolJson.SerializeHello("ACDEFHJK"));

    [Fact]
    public void Reply_Parsing()
    {
        Assert.True(ProtocolJson.ParseReply("{\"ok\":true}")!.Ok);
        var bad = ProtocolJson.ParseReply("{\"ok\":false,\"reason\":\"token\"}")!;
        Assert.False(bad.Ok);
        Assert.Equal("token", bad.Reason);
        Assert.Null(ProtocolJson.ParseReply("xx"));
    }

    [Fact]
    public void Argb_IsUppercaseEightHexDigits() => Assert.Equal("#FF14202C", ProtocolJson.Argb(255, 0x14, 0x20, 0x2C));
}

public class ConfigTests
{
    private static string Temp() => Directory.CreateTempSubdirectory("vietsub-test-").FullName;

    [Fact]
    public void Home_UsesEnvVar_ElseLocalAppData()
    {
        Assert.Equal(@"X:\vs", VietSubHome.Resolve(k => k == "VIETSUB_HOME" ? @"X:\vs" : null));
        var def = VietSubHome.Resolve(_ => null);
        Assert.EndsWith("VietSub", def);
    }

    [Fact]
    public void Load_MissingFile_GivesDefaults()
    {
        var c = EngineConfig.Load(Temp());
        Assert.Equal(47820, c.Tv.Port);
        Assert.Equal("auto", c.Display.CoverMode);
        Assert.Equal(24, c.Display.MinFontPx);
        Assert.Equal(1.0, c.Display.FontScale);
        Assert.Equal(1.2, c.Tuning.SplitGapFactor);
        Assert.Equal(100000, c.Tuning.DailyCapChars);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips_AndPartialFileKeepsDefaults()
    {
        var home = Temp();
        var c = new EngineConfig();
        c.Tv.Host = "192.168.1.50"; c.Tv.Token = "ACDEFHJK"; c.Display.HoldMs = 150;
        c.Save(home);
        var back = EngineConfig.Load(home);
        Assert.Equal("192.168.1.50", back.Tv.Host);
        Assert.Equal("ACDEFHJK", back.Tv.Token);
        Assert.Equal(150, back.Display.HoldMs);

        File.WriteAllText(EngineConfig.PathIn(home), "{\"display\":{\"minFontPx\":30}}");
        var partial = EngineConfig.Load(home);
        Assert.Equal(30, partial.Display.MinFontPx);
        Assert.Equal(1.0, partial.Display.FontScale); // không ghi ⇒ mặc định
        Assert.Equal(6, partial.Tuning.CellDiffThreshold);
    }

    [Theory]
    [InlineData("acdef-hjk", "ACDEFHJK")]
    [InlineData(" ACDE FHJK ", "ACDEFHJK")]
    public void PairCode_Normalize(string raw, string expected) => Assert.Equal(expected, PairCode.Normalize(raw));

    [Theory]
    [InlineData("ACDEFHJK", true)]
    [InlineData("acde-fhjk", true)]
    [InlineData("ACDEFHJ", false)]     // 7 ký tự
    [InlineData("ACDEFHJ0", false)]    // 0 không thuộc bảng
    [InlineData("ACDEFHJI", false)]    // I không thuộc bảng
    public void PairCode_IsValid(string raw, bool ok) => Assert.Equal(ok, PairCode.IsValid(raw));
}
