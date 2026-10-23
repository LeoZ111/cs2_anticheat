using System.Numerics;
using AntiCheat.Core.Fog;
using AntiCheat.Core.Geometry;

namespace AntiCheat.Core.Tests.Fog;

public class FogSystemTests
{
    // A wall in the plane x = 0, from y = -100 to 100, 200 units high.
    private static readonly Bvh Wall = new(new[]
    {
        new Triangle(new Vector3(0, -100, 0), new Vector3(0, 100, 0), new Vector3(0, 100, 200)),
        new Triangle(new Vector3(0, -100, 0), new Vector3(0, 100, 200), new Vector3(0, -100, 200)),
    });

    private static FogPlayer Player(int slot, int team, float x, float y, float vx = 0, float vy = 0, bool alive = true,
        float ping = 0f, int loudUntil = -1) =>
        new(slot, team, alive, new Vector3(x, y, 0), 64f, new Vector3(vx, vy, 0), ping, loudUntil);

    private static FogReason Decide(FogSystem fog, FogPlayer viewer, FogPlayer target, int tick = 0)
    {
        fog.Update(new[] { viewer, target }, tick);
        return fog.Decisions[(viewer.Slot, target.Slot)];
    }

    [Fact]
    public void EnemyBehindTheWallIsHidden()
    {
        var fog = new FogSystem(Wall);
        Assert.Equal(FogReason.Hidden, Decide(fog, Player(0, 2, -300, 0), Player(1, 3, 300, 0)));
        Assert.False(fog.ShouldSend(0, 1));
    }

    [Fact]
    public void EnemyBesideTheWallIsVisible()
    {
        var fog = new FogSystem(Wall);
        Assert.Equal(FogReason.Visible, Decide(fog, Player(0, 2, -300, 0), Player(1, 3, 300, 400)));
        Assert.True(fog.ShouldSend(0, 1));
    }

    [Fact]
    public void AShoulderPeekingOutIsEnough()
    {
        // The head is just behind the edge, but one shoulder sticks out.
        Assert.Equal(FogReason.Visible, Decide(new FogSystem(Wall), Player(0, 2, -300, 0), Player(1, 3, 300, 190)));
        Assert.Equal(FogReason.Hidden, Decide(new FogSystem(Wall), Player(0, 2, -300, 0), Player(1, 3, 300, 170)));
    }

    [Fact]
    public void TeammatesAndDeadPlayersAreAlwaysSent()
    {
        var fog = new FogSystem(Wall);
        Assert.Equal(FogReason.NotAnEnemy, Decide(fog, Player(0, 2, -300, 0), Player(1, 2, 300, 0)));
        Assert.Equal(FogReason.NotAnEnemy, Decide(fog, Player(0, 2, -300, 0, alive: false), Player(1, 3, 300, 0)));
    }

    [Fact]
    public void VeryCloseEnemiesAreAlwaysSent()
    {
        var fog = new FogSystem(Wall);
        Assert.Equal(FogReason.Near, Decide(fog, Player(0, 2, -50, 0), Player(1, 3, 50, 0)));
    }

    [Fact]
    public void AfterBeingSeenTheEnemyIsHeldBrieflyThenHidden()
    {
        var fog = new FogSystem(Wall, new FogSettings { HoldTicks = 8 });
        var viewer = Player(0, 2, -300, 0);
        Assert.Equal(FogReason.Visible, Decide(fog, viewer, Player(1, 3, 300, 400), tick: 1));
        Assert.Equal(FogReason.Holding, Decide(fog, viewer, Player(1, 3, 300, 0), tick: 5));
        Assert.Equal(FogReason.Hidden, Decide(fog, viewer, Player(1, 3, 300, 0), tick: 20));
    }

    [Fact]
    public void RunningEnemiesCanBeHeard()
    {
        var fog = new FogSystem(Wall);
        Assert.Equal(FogReason.Audible, Decide(fog, Player(0, 2, -300, 0), Player(1, 3, 300, 0, vy: 30, vx: 240)));
        Assert.Equal(FogReason.Hidden, Decide(fog, Player(0, 2, -300, 0), Player(1, 3, 300, 0, vx: 100)));
    }

    [Fact]
    public void ShootingEnemiesCanBeHeard()
    {
        var fog = new FogSystem(Wall);
        Assert.Equal(FogReason.Audible, Decide(fog, Player(0, 2, -300, 0), Player(1, 3, 300, 0, loudUntil: 10), tick: 5));
        Assert.Equal(FogReason.Hidden, Decide(fog, Player(0, 2, -300, 0), Player(1, 3, 300, 0, loudUntil: 4), tick: 5));
    }

    [Fact]
    public void FastEnemiesAreSentEarlyAgainstPopIn()
    {
        var settings = new FogSettings { FootstepRadius = 100f }; // isolate the prediction from footsteps
        var fog = new FogSystem(Wall, settings);
        var viewer = Player(0, 2, -800, 0, ping: 0.05f);
        Assert.Equal(FogReason.Hidden, Decide(fog, viewer, Player(1, 3, 800, 150, ping: 0.05f)));
        // Moving toward the edge: in about 0.2 s he will be visible, so he is sent now.
        Assert.Equal(FogReason.Visible, Decide(fog, viewer, Player(1, 3, 800, 150, vy: 400, ping: 0.05f)));
    }

    [Fact]
    public void UnknownPairsAreSent()
    {
        Assert.True(new FogSystem(Wall).ShouldSend(5, 6));
    }

    [Fact]
    public void BodyPointsSpanHeadToFeetAndBothShoulders()
    {
        var points = new FogSystem(Wall).BodyPoints(new Vector3(-300, 0, 64), new Vector3(300, 0, 0), 64f).ToList();
        Assert.Equal(5, points.Count);
        Assert.Contains(points, p => p.Z == 64f);
        Assert.Contains(points, p => p.Y > 10f);
        Assert.Contains(points, p => p.Y < -10f);
    }
}
