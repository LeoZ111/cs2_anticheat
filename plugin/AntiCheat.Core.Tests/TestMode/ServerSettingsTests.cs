using AntiCheat.Core.TestMode;

namespace AntiCheat.Core.Tests.TestMode;

public class ServerSettingsTests
{
    [Theory]
    [InlineData("mirage", "de_mirage")]
    [InlineData("DE_DUST2", "de_dust2")]
    [InlineData("office", "cs_office")]
    [InlineData("atlantis", null)]
    public void MapsAreFoundByShortOrFullName(string input, string? expected)
    {
        Assert.Equal(expected, ServerSettings.ResolveMap(input));
    }

    [Fact]
    public void MaxMoneyAllowsBuyingEverywhere()
    {
        var money = ServerSettings.ParseMoney("max")!;
        Assert.Equal(16000, money.StartMoney);
        Assert.Contains("mp_buy_anywhere 1", money.Commands);
    }

    [Fact]
    public void NormalMoneyIsTheCompetitiveDefault()
    {
        var money = ServerSettings.ParseMoney("normal")!;
        Assert.Equal(new[] { "mp_startmoney 800", "mp_maxmoney 16000", "mp_afterroundmoney 0", "mp_buy_anywhere 0", "mp_buytime 20" },
            money.Commands);
    }

    [Theory]
    [InlineData("5000", 5000, 16000)]
    [InlineData("30000", 30000, 30000)]
    public void AmountSetsStartAndMaxMoney(string input, int start, int max)
    {
        var money = ServerSettings.ParseMoney(input)!;
        Assert.Equal(start, money.StartMoney);
        Assert.Equal(max, money.MaxMoney);
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("70000")]
    [InlineData("viel")]
    public void InvalidMoneyGivesNull(string input)
    {
        Assert.Null(ServerSettings.ParseMoney(input));
    }

    [Theory]
    [InlineData("2", "2")]
    [InlineData("1,5", "1.5")]
    [InlineData("0.5", "0.5")]
    public void RoundTimeAcceptsCommaAndDot(string input, string expected)
    {
        Assert.Equal($"mp_roundtime {expected}", ServerSettings.RoundTime(input)![0]);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("61")]
    [InlineData("lang")]
    public void InvalidRoundTimeGivesNull(string input)
    {
        Assert.Null(ServerSettings.RoundTime(input));
    }

    [Fact]
    public void WarmupOnAndOff()
    {
        Assert.Equal("mp_warmup_end", ServerSettings.Warmup("off")![0]);
        Assert.Equal("mp_warmup_start", ServerSettings.Warmup("ON")![0]);
        Assert.Null(ServerSettings.Warmup("vielleicht"));
    }

    [Fact]
    public void NadeModesMatchBotImprover()
    {
        Assert.Equal("bot_nades max", ServerSettings.Nades("max")![0]);
        Assert.Null(ServerSettings.Nades("lots"));
    }

    [Fact]
    public void BotWeaponGivesLoadoutWithArmor()
    {
        Assert.Equal("awp tec9 vesthelm defuser", ServerSettings.BotLoadout("AWP"));
        Assert.Equal("revolver vesthelm defuser", ServerSettings.BotLoadout("r8"));
        Assert.Equal("", ServerSettings.BotLoadout("normal"));
        Assert.Null(ServerSettings.BotLoadout("banana"));
    }

    [Fact]
    public void WeaponNamesAreSorted()
    {
        var names = ServerSettings.BotWeaponNames;
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
        Assert.Contains("ak47", names);
    }
}
