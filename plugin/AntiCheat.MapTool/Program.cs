using System.Numerics;
using AntiCheat.Core.Geometry;
using ValvePak;
using ValveResourceFormat;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.ResourceTypes.RubikonPhysics;
using ValveResourceFormat.Serialization.KeyValues;

// Map tool: reads the collision geometry of a CS2 map and writes it as triangles (.actri).
// Usage: AntiCheat.MapTool <path to maps/<map>.vpk> <output.actri>
//        AntiCheat.MapTool check <map.actri> [pairs.csv]
if (args.Length >= 2 && args[0] == "check")
{
    return AntiCheat.MapTool.Check.Run(args[1], args.Length > 2 ? args[2] : null);
}
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: AntiCheat.MapTool <maps/<map>.vpk> <output.actri>  |  check <map.actri> [pairs.csv]");
    return 2;
}

string vpkPath = args[0];
string mapName = Path.GetFileNameWithoutExtension(vpkPath);
using var package = new Package();
package.Read(vpkPath);
var entry = package.FindEntry($"maps/{mapName}/world_physics.vmdl_c")
    ?? throw new FileNotFoundException($"maps/{mapName}/world_physics.vmdl_c not found in {vpkPath}");
package.ReadEntry(entry, out byte[] bytes);

using var resource = new Resource();
resource.Read(new MemoryStream(bytes));
var model = (Model)resource.DataBlock!;
PhysAggregateData phys = model.GetEmbeddedPhys() ?? throw new InvalidDataException("No physics data in the world model.");

// Layer name of a collision attribute, e.g. "default" or "playerclip".
var layerNames = phys.CollisionAttributes
    .Select(a => string.Join("+", a.GetArray<string>("m_InteractAsStrings") ?? Array.Empty<string>()).ToLowerInvariant())
    .Select(n => n.Length == 0 ? "default" : n)
    .ToList();
var layers = layerNames.Distinct().ToList();
ushort LayerOf(int attributeIndex) =>
    (ushort)layers.IndexOf(attributeIndex >= 0 && attributeIndex < layerNames.Count ? layerNames[attributeIndex] : "unknown");
if (!layers.Contains("unknown")) layers.Add("unknown");

var triangles = new List<Triangle>();
var triangleLayers = new List<ushort>();

for (int partIndex = 0; partIndex < phys.Parts.Length; partIndex++)
{
    Part part = phys.Parts[partIndex];
    Matrix4x4 transform = partIndex < phys.BindPose.Length ? phys.BindPose[partIndex] : Matrix4x4.Identity;

    foreach (var hull in part.Shape.Hulls)
    {
        ushort layer = LayerOf(hull.CollisionAttributeIndex);
        var vertices = hull.Shape.GetVertexPositions();
        var edges = hull.Shape.GetEdges();
        foreach (var face in hull.Shape.GetFaces())
        {
            // Walk the half edges around the face, then split the polygon into a fan of triangles.
            var polygon = new List<Vector3>();
            int start = face.Edge, e = start;
            do
            {
                polygon.Add(Vector3.Transform(vertices[edges[e].Origin], transform));
                e = edges[e].Next;
            } while (e != start && polygon.Count < 256);
            for (int i = 1; i + 1 < polygon.Count; i++)
            {
                triangles.Add(new Triangle(polygon[0], polygon[i], polygon[i + 1]));
                triangleLayers.Add(layer);
            }
        }
    }

    foreach (var mesh in part.Shape.Meshes)
    {
        ushort layer = LayerOf(mesh.CollisionAttributeIndex);
        var vertices = mesh.Shape.GetVertices();
        foreach (var t in mesh.Shape.GetTriangles())
        {
            triangles.Add(new Triangle(
                Vector3.Transform(vertices[t.X], transform),
                Vector3.Transform(vertices[t.Y], transform),
                Vector3.Transform(vertices[t.Z], transform)));
            triangleLayers.Add(layer);
        }
    }
}

var geometry = new MapGeometry(layers, triangles, triangleLayers);
using (var output = File.Create(args[1]))
{
    geometry.Write(output);
}

Console.WriteLine($"{mapName}: {phys.Parts.Length} parts, {triangles.Count} triangles -> {args[1]}");
foreach (var group in triangleLayers.GroupBy(l => l).OrderByDescending(g => g.Count()))
{
    Console.WriteLine($"  {layers[group.Key],-40} {group.Count(),8} triangles");
}
return 0;
