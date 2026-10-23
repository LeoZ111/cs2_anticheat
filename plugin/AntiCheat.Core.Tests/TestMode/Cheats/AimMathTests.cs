using System.Numerics;
using AntiCheat.Core.TestMode.Cheats;

namespace AntiCheat.Core.Tests.TestMode.Cheats;

public class AimMathTests
{
    private static readonly Vector3 Eye = new(0, 0, 64);

    [Fact]
    public void StraightAheadIsZero()
    {
        var a = AimMath.AnglesTo(Eye, new Vector3(100, 0, 64));
        Assert.Equal(0f, a.Pitch, 3);
        Assert.Equal(0f, a.Yaw, 3);
    }

    [Fact]
    public void LeftIsPlus90AndUpIsNegativePitch()
    {
        Assert.Equal(90f, AimMath.AnglesTo(Eye, new Vector3(0, 100, 64)).Yaw, 3);
        Assert.Equal(-45f, AimMath.AnglesTo(Eye, new Vector3(100, 0, 164)).Pitch, 3);
    }

    [Theory]
    [InlineData(170f, -170f, 20f)]
    [InlineData(-170f, 170f, -20f)]
    [InlineData(10f, 30f, 20f)]
    public void DeltaTakesTheShortWay(float from, float to, float expected)
    {
        Assert.Equal(expected, AimMath.Delta(from, to), 3);
    }

    [Fact]
    public void RageSnapsInOneTick()
    {
        var target = new ViewAngles(10f, 120f);
        var after = AimMath.Step(new ViewAngles(0f, 0f), target, AimProfile.Rage.Smoothing);
        Assert.Equal(0f, AimMath.Distance(after, target), 3);
    }

    [Fact]
    public void LegitMovesOnlyPartOfTheWay()
    {
        var target = new ViewAngles(0f, 10f);
        var after = AimMath.Step(new ViewAngles(0f, 0f), target, AimProfile.Legit.Smoothing);
        Assert.InRange(after.Yaw, 0.5f, 5f);
    }

    [Fact]
    public void PitchNeverGoesBeyondStraightUpOrDown()
    {
        var after = AimMath.Step(new ViewAngles(80f, 0f), new ViewAngles(120f, 0f), 1f);
        Assert.Equal(89f, after.Pitch);
    }

    [Fact]
    public void PickTargetTakesTheClosestInsideTheFieldOfView()
    {
        var targets = new[]
        {
            new AimTarget(1, new Vector3(100, 30, 64)),  // about 17 degrees off
            new AimTarget(2, new Vector3(100, 10, 64)),  // about 6 degrees off
            new AimTarget(3, new Vector3(-100, 0, 64)),  // behind
        };
        Assert.Equal(2, AimMath.PickTarget(Eye, new ViewAngles(0, 0), targets, AimProfile.Legit.FieldOfView)?.Id);
        Assert.Null(AimMath.PickTarget(Eye, new ViewAngles(0, 0), new[] { targets[0], targets[2] }, AimProfile.Legit.FieldOfView));
        Assert.Equal(3, AimMath.PickTarget(Eye, new ViewAngles(0, 180), targets, AimProfile.Rage.FieldOfView)?.Id);
    }
}
