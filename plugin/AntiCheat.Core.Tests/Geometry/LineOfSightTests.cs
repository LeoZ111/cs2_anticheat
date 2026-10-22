using System.Numerics;
using AntiCheat.Core.Geometry;

namespace AntiCheat.Core.Tests.Geometry;

public class LineOfSightTests
{
    // A square wall at x = 0, from y = -100 to 100 and z = 0 to 200, made of two triangles.
    private static readonly Triangle[] SquareWall =
    {
        new(new Vector3(0, -100, 0), new Vector3(0, 100, 0), new Vector3(0, 100, 200)),
        new(new Vector3(0, -100, 0), new Vector3(0, 100, 200), new Vector3(0, -100, 200)),
    };

    // Eye height of a standing player in CS2 is about 64 units.
    private static readonly Vector3 PlayerEye = new(-200, 0, 64);

    [Fact]
    public void EnemyBehindTheWallIsBlocked()
    {
        var sight = new Segment(PlayerEye, new Vector3(200, 0, 64));
        Assert.True(LineOfSight.IsBlocked(sight, SquareWall));
    }

    [Fact]
    public void EnemyNextToTheWallIsVisible()
    {
        var sight = new Segment(PlayerEye, new Vector3(200, 300, 64));
        Assert.False(LineOfSight.IsBlocked(sight, SquareWall));
    }

    [Fact]
    public void EnemyOnTheSameSideIsVisible()
    {
        var sight = new Segment(PlayerEye, new Vector3(-50, 0, 64));
        Assert.False(LineOfSight.IsBlocked(sight, SquareWall));
    }

    [Fact]
    public void EnemyLookingOverTheWallIsVisible()
    {
        var sight = new Segment(new Vector3(-200, 0, 300), new Vector3(200, 0, 300));
        Assert.False(LineOfSight.IsBlocked(sight, SquareWall));
    }

    [Fact]
    public void EmptyMapBlocksNothing()
    {
        var sight = new Segment(PlayerEye, new Vector3(200, 0, 64));
        Assert.False(LineOfSight.IsBlocked(sight, Array.Empty<Triangle>()));
    }
}
