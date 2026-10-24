using System.Diagnostics;
using System.Numerics;
using AntiCheat.Core.Geometry;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace AntiCheat.CounterStrikeSharp;

/// <summary>
/// The walls of the current map, shared by the fog and the simulated cheats.
/// Loads plugins/AntiCheat/maps/&lt;map&gt;.actri (made by AntiCheat.MapTool) on every map start.
/// </summary>
public sealed class MapSight
{
    private readonly BasePlugin _plugin;

    public MapSight(BasePlugin plugin)
    {
        _plugin = plugin;
    }

    public string Map { get; private set; } = "";

    /// <summary>Null if there is no geometry for this map.</summary>
    public Bvh? Walls { get; private set; }

    /// <summary>Raised after a map was loaded (Walls may be null).</summary>
    public event Action? Loaded;

    public void Register()
    {
        _plugin.RegisterListener<Listeners.OnMapStart>(Load);
        Load(Server.MapName);
    }

    /// <summary>Free line between two points? Without geometry we cannot know, so we say yes.</summary>
    public bool IsClear(Vector3 from, Vector3 to) => Walls is null || !Walls.IsBlocked(new Segment(from, to));

    private void Load(string map)
    {
        Map = map;
        Walls = null;
        string path = Path.Combine(_plugin.ModuleDirectory, "maps", map + ".actri");
        if (File.Exists(path))
        {
            var watch = Stopwatch.StartNew();
            MapGeometry geometry;
            using (var stream = File.OpenRead(path)) geometry = MapGeometry.Read(stream);
            Walls = new Bvh(geometry.Where(MapGeometry.BlocksSight));
            _plugin.Logger.LogInformation("Map walls: loaded {Map} ({Count} triangles) in {Ms} ms", map, geometry.Triangles.Count, watch.ElapsedMilliseconds);
        }
        else
        {
            _plugin.Logger.LogWarning("Map walls: no geometry for {Map} ({Path}). Fog and visibility checks are off.", map, path);
        }
        Loaded?.Invoke();
    }
}
