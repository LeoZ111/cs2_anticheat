using System.Numerics;
using AntiCheat.Core.Geometry;

namespace AntiCheat.Core.Tests.Geometry;

public class BvhTests
{
    private static Triangle RandomTriangle(Random r, float area = 3000f, float size = 120f)
    {
        Vector3 c = new((float)r.NextDouble() * area, (float)r.NextDouble() * area, (float)r.NextDouble() * 400f);
        Vector3 Jitter() => new((float)(r.NextDouble() - 0.5) * size, (float)(r.NextDouble() - 0.5) * size, (float)(r.NextDouble() - 0.5) * size);
        return new Triangle(c + Jitter(), c + Jitter(), c + Jitter());
    }

    [Fact]
    public void SameAnswerAsTestingEveryTriangle()
    {
        var r = new Random(12);
        var triangles = Enumerable.Range(0, 3000).Select(_ => RandomTriangle(r)).ToList();
        var bvh = new Bvh(triangles);
        for (int i = 0; i < 2000; i++)
        {
            var a = RandomTriangle(r, size: 0).A;
            var b = RandomTriangle(r, size: 0).A;
            var segment = new Segment(a, b);
            Assert.Equal(LineOfSight.IsBlocked(segment, triangles), bvh.IsBlocked(segment));
        }
    }

    [Fact]
    public void WallBlocksAndGapDoesNot()
    {
        var wall = new[]
        {
            new Triangle(new Vector3(0, -100, 0), new Vector3(0, 100, 0), new Vector3(0, 100, 200)),
            new Triangle(new Vector3(0, -100, 0), new Vector3(0, 100, 200), new Vector3(0, -100, 200)),
        };
        var bvh = new Bvh(wall);
        Assert.True(bvh.IsBlocked(new Segment(new Vector3(-200, 0, 64), new Vector3(200, 0, 64))));
        Assert.False(bvh.IsBlocked(new Segment(new Vector3(-200, 300, 64), new Vector3(200, 300, 64))));
    }

    [Fact]
    public void EmptyMapBlocksNothing()
    {
        Assert.False(new Bvh(Array.Empty<Triangle>()).IsBlocked(new Segment(Vector3.Zero, Vector3.One)));
    }

    [Fact]
    public void AxisParallelSegmentsWork()
    {
        var floor = new[] { new Triangle(new Vector3(-100, -100, 0), new Vector3(100, -100, 0), new Vector3(0, 100, 0)) };
        var bvh = new Bvh(floor);
        Assert.True(bvh.IsBlocked(new Segment(new Vector3(0, 0, 50), new Vector3(0, 0, -50))));
        Assert.False(bvh.IsBlocked(new Segment(new Vector3(0, 0, 50), new Vector3(0, 0, 10))));
    }

    [Fact]
    public void MapGeometryFileRoundTrip()
    {
        var geometry = new MapGeometry(new[] { "default", "playerclip" },
            new[] { new Triangle(Vector3.Zero, Vector3.UnitX, Vector3.UnitY), new Triangle(Vector3.One, Vector3.UnitZ, Vector3.UnitY) },
            new ushort[] { 0, 1 });
        using var stream = new MemoryStream();
        geometry.Write(stream);
        stream.Position = 0;
        var read = MapGeometry.Read(stream);
        Assert.Equal(geometry.Layers, read.Layers);
        Assert.Equal(geometry.Triangles, read.Triangles);
        Assert.Single(read.Where(layer => layer == "default"));
    }
}
