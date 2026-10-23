using System.Numerics;
using System.Text;

namespace AntiCheat.Core.Geometry;

/// <summary>
/// Collision triangles of a map, each tagged with a layer (what it blocks, e.g. "default" or "playerclip").
/// File format (.actri, little endian):
///   "ACT1", int layerCount, layerCount x (int byteLength, UTF-8 name), int triangleCount,
///   triangleCount x (9 floats: A, B, C, ushort layer)
/// </summary>
public sealed class MapGeometry
{
    private static readonly byte[] Magic = "ACT1"u8.ToArray();

    public IReadOnlyList<string> Layers { get; }
    public IReadOnlyList<Triangle> Triangles { get; }
    public IReadOnlyList<ushort> TriangleLayers { get; }

    public MapGeometry(IReadOnlyList<string> layers, IReadOnlyList<Triangle> triangles, IReadOnlyList<ushort> triangleLayers)
    {
        if (triangles.Count != triangleLayers.Count) throw new ArgumentException("One layer per triangle.");
        Layers = layers;
        Triangles = triangles;
        TriangleLayers = triangleLayers;
    }

    /// <summary>
    /// Layers that block sight. Not: invisible walls for players/NPCs/grenades, and things bullets pass through.
    /// </summary>
    public static bool BlocksSight(string layer) =>
        !layer.Contains("playerclip") && !layer.Contains("npcclip") && !layer.Contains("grenadeclip") && !layer.Contains("passbullets");

    /// <summary>Only the triangles whose layer passes the filter, e.g. everything that blocks sight.</summary>
    public IReadOnlyList<Triangle> Where(Func<string, bool> keepLayer)
    {
        var keep = Layers.Select(keepLayer).ToArray();
        return Triangles.Where((_, i) => keep[TriangleLayers[i]]).ToList();
    }

    public void Write(Stream stream)
    {
        using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(Layers.Count);
        foreach (string layer in Layers)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(layer);
            w.Write(bytes.Length);
            w.Write(bytes);
        }
        w.Write(Triangles.Count);
        for (int i = 0; i < Triangles.Count; i++)
        {
            foreach (Vector3 v in new[] { Triangles[i].A, Triangles[i].B, Triangles[i].C })
            {
                w.Write(v.X);
                w.Write(v.Y);
                w.Write(v.Z);
            }
            w.Write(TriangleLayers[i]);
        }
    }

    public static MapGeometry Read(Stream stream)
    {
        using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        if (!r.ReadBytes(4).SequenceEqual(Magic)) throw new InvalidDataException("Not a map geometry file (ACT1).");
        var layers = new List<string>();
        int layerCount = r.ReadInt32();
        for (int i = 0; i < layerCount; i++) layers.Add(Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32())));
        int count = r.ReadInt32();
        var triangles = new List<Triangle>(count);
        var triangleLayers = new List<ushort>(count);
        for (int i = 0; i < count; i++)
        {
            Vector3 a = new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            Vector3 b = new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            Vector3 c = new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            triangles.Add(new Triangle(a, b, c));
            triangleLayers.Add(r.ReadUInt16());
        }
        return new MapGeometry(layers, triangles, triangleLayers);
    }
}
