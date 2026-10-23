using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using AntiCheat.Core.Geometry;

namespace AntiCheat.MapTool;

/// <summary>
/// "check" mode: loads a converted map, measures how fast sight lines are,
/// and optionally compares them with the game's own visibility from real matches (pairs CSV).
/// </summary>
public static class Check
{
    public static int Run(string mapFile, string? pairsCsv)
    {
        var watch = Stopwatch.StartNew();
        MapGeometry geometry;
        using (var stream = File.OpenRead(mapFile)) geometry = MapGeometry.Read(stream);
        var sight = geometry.Where(MapGeometry.BlocksSight);
        Console.WriteLine($"loaded {geometry.Triangles.Count} triangles ({sight.Count} block sight) in {watch.ElapsedMilliseconds} ms");

        watch.Restart();
        var bvh = new Bvh(sight);
        Console.WriteLine($"BVH built: {bvh.NodeCount} nodes in {watch.ElapsedMilliseconds} ms");

        Benchmark(bvh, sight);
        if (pairsCsv is not null) CompareWithGame(bvh, pairsCsv);
        return 0;
    }

    private static void Benchmark(Bvh bvh, IReadOnlyList<Triangle> triangles)
    {
        // Random segments inside the map's bounding box, at player height ranges.
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var t in triangles)
        {
            min = Vector3.Min(min, Vector3.Min(t.A, Vector3.Min(t.B, t.C)));
            max = Vector3.Max(max, Vector3.Max(t.A, Vector3.Max(t.B, t.C)));
        }
        var random = new Random(1);
        Vector3 Point() => new(min.X + (float)random.NextDouble() * (max.X - min.X), min.Y + (float)random.NextDouble() * (max.Y - min.Y),
            min.Z + (float)random.NextDouble() * Math.Min(400f, max.Z - min.Z));
        var segments = Enumerable.Range(0, 200_000).Select(_ => new Segment(Point(), Point())).ToArray();

        var watch = Stopwatch.StartNew();
        int blocked = 0;
        foreach (var s in segments) if (bvh.IsBlocked(s)) blocked++;
        double micro = watch.Elapsed.TotalMilliseconds * 1000 / segments.Length;
        Console.WriteLine($"{segments.Length} random sight lines: {micro:F2} Âµs each ({blocked * 100 / segments.Length} % blocked)");
        Console.WriteLine($"  per tick budget 0.5 ms = about {(int)(500 / micro)} sight lines");
    }

    // CSV columns: ax,ay,az,bx,by,bz,spotted (eye positions; spotted = game says a has spotted b)
    private static void CompareWithGame(Bvh bvh, string csv)
    {
        long clearSpotted = 0, clearNot = 0, blockedSpotted = 0, blockedNot = 0;
        foreach (string line in File.ReadLines(csv).Skip(1))
        {
            var f = line.Split(',');
            if (f.Length < 7) continue;
            float P(int i) => float.Parse(f[i], CultureInfo.InvariantCulture);
            var segment = new Segment(new Vector3(P(0), P(1), P(2)), new Vector3(P(3), P(4), P(5)));
            bool spotted = f[6].Trim() is "1" or "True" or "true";
            bool clear = !bvh.IsBlocked(segment);
            if (clear && spotted) clearSpotted++;
            else if (clear) clearNot++;
            else if (spotted) blockedSpotted++;
            else blockedNot++;
        }
        long total = clearSpotted + clearNot + blockedSpotted + blockedNot;
        Console.WriteLine($"compared {total} player pairs with the game's visibility:");
        Console.WriteLine($"  our line clear   : {clearSpotted + clearNot,8}  of these the game says spotted {Pct(clearSpotted, clearSpotted + clearNot)}");
        Console.WriteLine($"  our line blocked : {blockedSpotted + blockedNot,8}  of these the game says spotted {Pct(blockedSpotted, blockedSpotted + blockedNot)}");
    }

    private static string Pct(long part, long whole) => whole == 0 ? "-" : $"{100.0 * part / whole:F1} %";
}
