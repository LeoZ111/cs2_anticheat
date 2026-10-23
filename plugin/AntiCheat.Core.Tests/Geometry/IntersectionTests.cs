using System.Numerics;
using AntiCheat.Core.Geometry;

namespace AntiCheat.Core.Tests.Geometry;

public class IntersectionTests
{
    // A wall-like triangle standing in the plane x = 0.
    private static readonly Triangle Wall = new(
        new Vector3(0, -100, 0),
        new Vector3(0, 100, 0),
        new Vector3(0, 0, 200));

    [Fact]
    public void SegmentThroughTheMiddleHits()
    {
        var segment = new Segment(new Vector3(-50, 0, 50), new Vector3(50, 0, 50));
        Assert.True(Intersection.SegmentHitsTriangle(segment, Wall));
    }

    [Fact]
    public void SegmentFromTheBackSideAlsoHits()
    {
        var segment = new Segment(new Vector3(50, 0, 50), new Vector3(-50, 0, 50));
        Assert.True(Intersection.SegmentHitsTriangle(segment, Wall));
    }

    [Fact]
    public void SegmentThatStopsBeforeTheWallMisses()
    {
        var segment = new Segment(new Vector3(-50, 0, 50), new Vector3(-1, 0, 50));
        Assert.False(Intersection.SegmentHitsTriangle(segment, Wall));
    }

    [Fact]
    public void SegmentThatStartsBehindTheWallMisses()
    {
        var segment = new Segment(new Vector3(1, 0, 50), new Vector3(50, 0, 50));
        Assert.False(Intersection.SegmentHitsTriangle(segment, Wall));
    }

    [Fact]
    public void SegmentPassingBesideTheTriangleMisses()
    {
        var segment = new Segment(new Vector3(-50, 150, 50), new Vector3(50, 150, 50));
        Assert.False(Intersection.SegmentHitsTriangle(segment, Wall));
    }

    [Fact]
    public void SegmentParallelToTheWallMisses()
    {
        var segment = new Segment(new Vector3(-10, -50, 50), new Vector3(-10, 50, 50));
        Assert.False(Intersection.SegmentHitsTriangle(segment, Wall));
    }

    [Theory]
    [InlineData(40, true)]   // just inside the slanted edge
    [InlineData(60, false)]  // just outside it
    public void PointsNearTheSlantedEdge(float y, bool expectedHit)
    {
        // At height z = 100 the wall spans y from -50 to 50.
        var segment = new Segment(new Vector3(-50, y, 100), new Vector3(50, y, 100));
        Assert.Equal(expectedHit, Intersection.SegmentHitsTriangle(segment, Wall));
    }

    [Fact]
    public void RayReportsWhereItHits()
    {
        bool hit = Intersection.RayHitsTriangle(new Vector3(-30, 0, 50), new Vector3(1, 0, 0), Wall, out float t);
        Assert.True(hit);
        Assert.Equal(30f, t, precision: 4);
    }
}
