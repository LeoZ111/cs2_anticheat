using AntiCheat.Core.TestMode.Cheats;

namespace AntiCheat.Core.Tests.TestMode.Cheats;

public class CheatCatalogTests
{
    [Fact]
    public void EveryKindHasOneEntry()
    {
        Assert.Equal(Enum.GetValues<CheatKind>().Order(), CheatCatalog.All.Select(c => c.Kind).Order());
    }

    [Theory]
    [InlineData("esp", CheatKind.Esp)]
    [InlineData("AIM_RAGE", CheatKind.AimRage)]
    public void CheatsAreFoundByName(string name, CheatKind kind)
    {
        Assert.Equal(kind, CheatCatalog.Find(name)?.Kind);
    }

    [Theory]
    [InlineData("", false, true)]
    [InlineData("", true, false)]
    [InlineData("on", true, true)]
    [InlineData("aus", true, false)]
    public void SwitchParsesOnOffAndToggle(string text, bool current, bool expected)
    {
        Assert.Equal(expected, CheatCatalog.ParseSwitch(text, current));
    }

    [Fact]
    public void UnknownSwitchGivesNull()
    {
        Assert.Null(CheatCatalog.ParseSwitch("vielleicht", false));
    }

    [Fact]
    public void OnlyOneAimbotAtATime()
    {
        var cheats = new PlayerCheats();
        cheats.Set(3, CheatKind.AimLegit, true);
        cheats.Set(3, CheatKind.AimRage, true);
        Assert.False(cheats.IsOn(3, CheatKind.AimLegit));
        Assert.True(cheats.IsOn(3, CheatKind.AimRage));
    }

    [Fact]
    public void CheatsBelongToOnePlayer()
    {
        var cheats = new PlayerCheats();
        cheats.Set(1, CheatKind.Esp, true);
        Assert.False(cheats.IsOn(2, CheatKind.Esp));
        Assert.Equal(new[] { 1 }, cheats.SlotsWith(CheatKind.Esp));
        cheats.Clear(1);
        Assert.False(cheats.AnyoneHas(CheatKind.Esp));
    }
}
