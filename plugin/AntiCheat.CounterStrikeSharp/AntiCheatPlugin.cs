using System.Numerics;
using AntiCheat.Core.Geometry;
using AntiCheat.Core.TestMode;
using AntiCheat.CounterStrikeSharp.Fog;
using AntiCheat.CounterStrikeSharp.TestMode;
using AntiCheat.CounterStrikeSharp.TestMode.Cheats;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Microsoft.Extensions.Logging;

namespace AntiCheat.CounterStrikeSharp;

/// <summary>
/// Adapter between the CS2 server (CounterStrikeSharp) and the engine-independent core.
/// For now it only proves that the plugin and the core load on the server.
/// </summary>
public class AntiCheatPlugin : BasePlugin
{
    public override string ModuleName => "AntiCheat";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "LeoZ111";
    public override string ModuleDescription => "Server-side anti-cheat (fog of war, decoys)";

    // Simulated cheats only work while this is 1. Set it in the test server's config, never on a real server.
    public FakeConVar<bool> TestMode = new("ac_testmode", "Enables test-mode features like simulated cheats", false);

    // Fog of war. Off by default until it is tested well.
    public FakeConVar<bool> FogEnabled = new("ac_fog", "Fog of war: do not send enemies a player cannot see or hear", false);

    private CheatsModule? _cheats;
    private FogModule? _fog;

    public override void Load(bool hotReload)
    {
        // Logger writes to the server console and to addons/counterstrikesharp/logs.
        Logger.LogInformation("Hallo! Plugin geladen. Kern-Selbsttest: {Result}", CoreSelfTest() ? "OK" : "FEHLER");

        // The fog must register before the cheats: its CheckTransmit runs first, so the simulated cheats
        // only see what the fog lets through, like real cheats.
        var sight = new MapSight(this);
        _fog = new FogModule(this, sight,() => FogEnabled.Value, on => FogEnabled.Value = on, () => TestMode.Value);
        _fog.Register();

        // Test-mode commands. Only for our own test server.
        BotSkillCommand.Register(this);
        BotPositionsCommand.Register(this);
        SettingsCommands.Register(this);
        _cheats = new CheatsModule(this, sight, () => TestMode.Value);
        _cheats.Register();

        // Load the walls last, so the fog and the cheats already listen for them.
        sight.Register();

        // Every chat command must be listed here, so "!" and "!<beginning>" can show it.
        ChatHelp.Register(this, SettingsCommands.Infos
            .Append(new ChatCommandInfo("bot_skill", "[" + string.Join("|", BotSkill.AllNames.Split(", ")) + "]", "Bot-Stärke einstellen (ohne Angabe: Menü)"))
            .Append(CheatsModule.Info)
            .Append(FogModule.Info)
            .ToList());
    }

    public override void Unload(bool hotReload)
    {
        _cheats?.Unload();
    }

    // A tiny line-of-sight check, so we see in the server console that the core runs there too.
    private static bool CoreSelfTest()
    {
        var wall = new Triangle(new Vector3(0, -100, 0), new Vector3(0, 100, 0), new Vector3(0, 0, 200));
        var throughWall = new Segment(new Vector3(-50, 0, 50), new Vector3(50, 0, 50));
        var besideWall = new Segment(new Vector3(-50, 150, 50), new Vector3(50, 150, 50));
        return LineOfSight.IsBlocked(throughWall, new[] { wall }) && !LineOfSight.IsBlocked(besideWall, new[] { wall });
    }
}
