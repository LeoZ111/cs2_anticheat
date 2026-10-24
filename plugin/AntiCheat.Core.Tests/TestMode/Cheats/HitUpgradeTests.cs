using AntiCheat.Core.TestMode.Cheats;

namespace AntiCheat.Core.Tests.TestMode.Cheats;

public class HitUpgradeTests
{
    [Fact]
    public void RageTurnsAChestHitIntoAHeadshot()
    {
        var hit = HitUpgrade.Apply(CheatKind.AimRage, new Hit(27f, HitGroup.Chest));
        Assert.Equal(HitGroup.Head, hit.Group);
        Assert.Equal(108f, hit.Damage, 3);
    }

    [Fact]
    public void RageUsesTheBaseDamageOfLegHits()
    {
        // 0.75 x base = 15  ->  base 20  ->  head 80
        var hit = HitUpgrade.Apply(CheatKind.AimRage, new Hit(15f, HitGroup.LeftLeg));
        Assert.Equal(80f, hit.Damage, 3);
    }

    [Fact]
    public void HeadshotsStayAsTheyAre()
    {
        var head = new Hit(100f, HitGroup.Head);
        Assert.Equal(head, HitUpgrade.Apply(CheatKind.AimRage, head));
        Assert.Equal(head, HitUpgrade.Apply(CheatKind.AimLegit, head));
    }

    [Fact]
    public void LegitMakesBodyHitsAbitStronger()
    {
        var hit = HitUpgrade.Apply(CheatKind.AimLegit, new Hit(20f, HitGroup.Stomach));
        Assert.Equal(HitGroup.Stomach, hit.Group);
        Assert.Equal(30f, hit.Damage, 3);
    }

    [Fact]
    public void OtherCheatsDoNotChangeHits()
    {
        var hit = new Hit(20f, HitGroup.Chest);
        Assert.Equal(hit, HitUpgrade.Apply(CheatKind.Esp, hit));
    }

    [Theory]
    [InlineData(0.5f, 1000f, true)]
    [InlineData(3f, 1000f, false)]
    [InlineData(3f, 200f, true)]
    [InlineData(0.9f, 5000f, true)] // never below the 1 degree minimum
    public void OnBodyDependsOnDistance(float error, float distance, bool expected)
    {
        Assert.Equal(expected, AimMath.IsOnBody(error, distance));
    }
}
