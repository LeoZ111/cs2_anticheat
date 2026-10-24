using System.Diagnostics;
using System.Numerics;
using AntiCheat.Core.Fog;
using AntiCheat.Core.Geometry;
using AntiCheat.Core.TestMode;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace AntiCheat.CounterStrikeSharp.Fog;

/// <summary>
/// Fog of war on the server: enemies a player cannot see or hear are not sent to his game (CheckTransmit).
/// The decisions come from the engine-independent FogSystem; this class only feeds it and applies it.
/// Map geometry: plugins/AntiCheat/maps/&lt;map&gt;.actri (made by AntiCheat.MapTool).
/// </summary>
public sealed class FogModule
{
    public static readonly ChatCommandInfo Info = new("fog", "[on|off]", "Fog of War an/aus (nur Testserver)");

    private const float GunshotAudibleSeconds = 1.0f;

    private readonly BasePlugin _plugin;
    private readonly MapSight _sight;
    private readonly Func<bool> _enabled;
    private readonly Action<bool> _setEnabled;
    private readonly Func<bool> _testMode;
    private FogSystem? _fog;
    private string _loadedMap = "";
    private readonly Dictionary<int, int> _loudUntilTick = new();

    // Stats since the fog was switched on
    private long _ticks, _decisions, _hidden, _lines;
    private double _totalMs, _maxMs;

    public FogModule(BasePlugin plugin, MapSight sight, Func<bool> enabled, Action<bool> setEnabled, Func<bool> testMode)
    {
        _plugin = plugin;
        _sight = sight;
        _enabled = enabled;
        _setEnabled = setEnabled;
        _testMode = testMode;
    }

    public void Register()
    {
        _sight.Loaded += () =>
        {
            _loadedMap = _sight.Map;
            _fog = _sight.Walls is null ? null : new FogSystem(_sight.Walls);
        };
        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
        _plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        _plugin.RegisterListener<Listeners.OnClientDisconnect>(slot => _fog?.Forget(slot));
        _plugin.RegisterEventHandler<EventWeaponFire>((e, _) =>
        {
            if (e.Userid is { IsValid: true } shooter)
            {
                _loudUntilTick[shooter.Slot] = Server.TickCount + (int)(GunshotAudibleSeconds * 64);
            }
            return HookResult.Continue;
        });
        _plugin.AddCommand("css_fog", Info.Description, OnFogCommand);
        _plugin.AddCommand("css_fog_stats", "Fog statistics", (_, command) => command.ReplyToCommand(StatsText()));
    }

    private void OnFogCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (!_testMode())
        {
            command.ReplyToCommand($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Nur im Testmodus (ac_testmode 1).");
            return;
        }
        string arg = command.ArgCount > 1 ? command.GetArg(1).ToLowerInvariant() : "";
        bool on = arg switch { "on" or "an" or "1" => true, "off" or "aus" or "0" => false, _ => !_enabled() };
        _setEnabled(on);
        ResetStats();
        string geometry = _fog is null ? $" {ChatColors.Red}(keine Karten-Geometrie für {_loadedMap}!)" : "";
        Server.PrintToChatAll($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} Fog of War: {(on ? $"{ChatColors.Green}AN" : $"{ChatColors.Grey}aus")}{geometry}");
    }

    private void OnTick()
    {
        if (!_enabled() || _fog is null) return;
        var watch = Stopwatch.StartNew();
        int tick = Server.TickCount;
        var players = new List<FogPlayer>();
        foreach (var controller in Utilities.GetPlayers())
        {
            if (!controller.IsValid) continue;
            var pawn = controller.PlayerPawn.Value;
            bool alive = pawn is { IsValid: true } && pawn.LifeState == (byte)LifeState_t.LIFE_ALIVE && pawn.AbsOrigin is not null;
            Vector3 origin = alive ? ToVector3(pawn!.AbsOrigin!) : Vector3.Zero;
            Vector3 velocity = alive ? ToVector3(pawn!.AbsVelocity) : Vector3.Zero;
            float eye = alive ? pawn!.ViewOffset.Z : 64f;
            float ping = controller.IsBot ? 0f : controller.Ping / 1000f;
            int loud = _loudUntilTick.TryGetValue(controller.Slot, out int until) ? until : -1;
            players.Add(new FogPlayer(controller.Slot, controller.TeamNum, alive, origin, eye, velocity, ping, loud));
        }
        _fog.Update(players, tick);
        double ms = watch.Elapsed.TotalMilliseconds;

        _ticks++;
        _totalMs += ms;
        _maxMs = Math.Max(_maxMs, ms);
        _lines += _fog.LinesLastUpdate;
        foreach (var reason in _fog.Decisions.Values)
        {
            if (reason == FogReason.NotAnEnemy) continue;
            _decisions++;
            if (reason == FogReason.Hidden) _hidden++;
        }
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (!_enabled() || _fog is null) return;
        var players = Utilities.GetPlayers().Where(p => p.IsValid).ToList();
        foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
        {
            if (viewer is null || !viewer.IsValid) continue;
            foreach (var target in players)
            {
                if (target.Slot == viewer.Slot || _fog.ShouldSend(viewer.Slot, target.Slot)) continue;
                var pawn = target.PlayerPawn.Value;
                if (pawn is null || !pawn.IsValid) continue;
                info.TransmitEntities.Remove(pawn.Index);
                // His weapons would give his position away too.
                var weapons = pawn.WeaponServices?.MyWeapons;
                if (weapons is null) continue;
                foreach (var handle in weapons)
                {
                    if (handle.Value is { IsValid: true } weapon) info.TransmitEntities.Remove(weapon.Index);
                }
            }
        }
    }

    private void ResetStats()
    {
        _ticks = _decisions = _hidden = _lines = 0;
        _totalMs = _maxMs = 0;
    }

    private string StatsText()
    {
        if (_ticks == 0) return $"[AntiCheat] Fog: {(_enabled() ? "on" : "off")}, map {_loadedMap}, geometry {(_fog is null ? "missing" : "loaded")}, no ticks yet.";
        return $"[AntiCheat] Fog: {_ticks} ticks, avg {_totalMs / _ticks:F3} ms, max {_maxMs:F3} ms, " +
               $"{(double)_lines / _ticks:F0} sight lines/tick, hidden {100.0 * _hidden / Math.Max(1, _decisions):F1} % of enemy pairs";
    }

    private static Vector3 ToVector3(global::CounterStrikeSharp.API.Modules.Utils.Vector v) => new(v.X, v.Y, v.Z);
}
