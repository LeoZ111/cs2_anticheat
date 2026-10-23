using System.Drawing;
using System.Numerics;
using AntiCheat.Core.TestMode;
using AntiCheat.Core.TestMode.Cheats;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace AntiCheat.CounterStrikeSharp.TestMode.Cheats;

/// <summary>
/// Simulated cheats a player can switch on for himself: !cheat esp, !cheat aim_rage, ...
/// Everything runs on our test server; nothing touches a game client. Only active while ac_testmode is 1.
/// Like real cheats, they only use enemies the server actually sends to the cheater (CheckTransmit).
/// </summary>
public sealed class CheatsModule
{
    public static readonly ChatCommandInfo Info = new("cheat", "<" + string.Join("|", CheatCatalog.All.Select(c => c.Name)) + "> [on|off]",
        "Simulierte Cheats für dich (nur Testserver)");

    private const float SpeedMultiplier = 1.5f;

    private readonly BasePlugin _plugin;
    private readonly Func<bool> _testMode;
    private readonly PlayerCheats _cheats = new();
    private readonly EspGlows _glows = new();

    // Which enemies were sent to each player in the last CheckTransmit. A real cheat only knows these.
    private readonly Dictionary<int, HashSet<uint>> _transmittedTo = new();

    public CheatsModule(BasePlugin plugin, Func<bool> testMode)
    {
        _plugin = plugin;
        _testMode = testMode;
    }

    public void Register()
    {
        _plugin.AddCommand("css_cheat", Info.Description, OnCommand);
        _plugin.RegisterListener<Listeners.OnTick>(OnTick);
        _plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
        _plugin.RegisterListener<Listeners.OnClientDisconnect>(slot => TurnAllOff(slot));
        _plugin.RegisterEventHandler<EventRoundStart>((_, _) => { _glows.RemoveAll(); return HookResult.Continue; });
        _plugin.AddTimer(0.5f, RefreshGlows, global::CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }

    public void Unload() => _glows.RemoveAll();

    private void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player is null || !player.IsValid)
        {
            command.ReplyToCommand("[AntiCheat] !cheat only works for players.");
            return;
        }
        if (!_testMode())
        {
            command.ReplyToCommand($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Cheats gibt es nur im Testmodus (ac_testmode 1).");
            return;
        }

        string name = command.ArgCount > 1 ? command.GetArg(1) : "";
        CheatInfo? cheat = CheatCatalog.Find(name);
        if (cheat is null)
        {
            player.PrintToChat($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} Cheats (!cheat <name> [on|off]):");
            foreach (var c in CheatCatalog.All)
            {
                string state = _cheats.IsOn(player.Slot, c.Kind) ? $"{ChatColors.Green}AN" : $"{ChatColors.Grey}aus";
                player.PrintToChat($"  {ChatColors.Gold}{c.Name}{ChatColors.Default} [{state}{ChatColors.Default}] {c.Description}");
            }
            return;
        }

        string switchText = command.ArgCount > 2 ? command.GetArg(2) : "";
        bool? on = CheatCatalog.ParseSwitch(switchText, _cheats.IsOn(player.Slot, cheat.Kind));
        if (on is null)
        {
            player.PrintToChat($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Benutzung: !cheat {cheat.Name} [on|off]");
            return;
        }

        _cheats.Set(player.Slot, cheat.Kind, on.Value);
        if (cheat.Kind == CheatKind.Bhop) ApplyBhop();
        if (cheat.Kind == CheatKind.Speed && !on.Value) ResetSpeed(player);
        if (cheat.Kind == CheatKind.Esp && !on.Value) _glows.RemoveFor(player.Slot);

        player.PrintToChat($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} {cheat.Name}: {(on.Value ? $"{ChatColors.Green}AN" : $"{ChatColors.Grey}aus")}");
        _plugin.Logger.LogInformation("cheat {Cheat} {State} for {Player}", cheat.Name, on.Value ? "on" : "off", player.PlayerName);
    }

    private void TurnAllOff(int slot)
    {
        _glows.RemoveFor(slot);
        _cheats.Clear(slot);
        _transmittedTo.Remove(slot);
        ApplyBhop();
    }

    // ---------- per tick: radar, aimbot, speed ----------

    private void OnTick()
    {
        if (!_testMode()) return;

        foreach (int slot in _cheats.SlotsWith(CheatKind.Radar).ToList()) ApplyRadar(slot);
        foreach (int slot in _cheats.SlotsWith(CheatKind.AimLegit).ToList()) ApplyAim(slot, AimProfile.Legit);
        foreach (int slot in _cheats.SlotsWith(CheatKind.AimRage).ToList()) ApplyAim(slot, AimProfile.Rage);
        foreach (int slot in _cheats.SlotsWith(CheatKind.Speed).ToList()) ApplySpeed(slot);
    }

    private IEnumerable<CCSPlayerPawn> KnownEnemyPawns(CCSPlayerController cheater)
    {
        _transmittedTo.TryGetValue(cheater.Slot, out var sent);
        foreach (var other in Utilities.GetPlayers())
        {
            if (!other.IsValid || other.Slot == cheater.Slot || other.TeamNum == cheater.TeamNum || other.TeamNum < 2) continue;
            var pawn = other.PlayerPawn.Value;
            if (pawn is null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) continue;
            if (sent is not null && !sent.Contains(pawn.Index)) continue; // not sent to the cheater -> a real cheat would not know it
            yield return pawn;
        }
    }

    private void ApplyRadar(int slot)
    {
        var cheater = Utilities.GetPlayerFromSlot(slot);
        if (cheater is null || !cheater.IsValid) return;
        foreach (var pawn in KnownEnemyPawns(cheater))
        {
            pawn.EntitySpottedState.Spotted = true;
            Span<uint> mask = pawn.EntitySpottedState.SpottedByMask;
            mask[slot / 32] |= 1u << (slot % 32);
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState");
        }
    }

    private void ApplyAim(int slot, AimProfile profile)
    {
        var cheater = Utilities.GetPlayerFromSlot(slot);
        var pawn = cheater?.PlayerPawn.Value;
        if (cheater is null || pawn is null || !pawn.IsValid || pawn.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
        if (pawn.AbsOrigin is null) return;

        Vector3 eye = ToVector3(pawn.AbsOrigin) + new Vector3(0, 0, pawn.ViewOffset.Z);
        var current = new ViewAngles(pawn.EyeAngles.X, pawn.EyeAngles.Y);
        var targets = KnownEnemyPawns(cheater)
            .Where(p => p.AbsOrigin is not null)
            .Select(p => new AimTarget((int)p.Index, HeadPoint(p)));

        AimTarget? target = AimMath.PickTarget(eye, current, targets, profile.FieldOfView);
        if (target is null) return;

        ViewAngles next = AimMath.Step(current, AimMath.AnglesTo(eye, target.Value.Point), profile.Smoothing);
        pawn.Teleport(null, new QAngle(next.Pitch, next.Yaw, 0), null);
    }

    // Head height: eye height of the enemy, minus a little so we hit the head, not above it.
    private static Vector3 HeadPoint(CCSPlayerPawn pawn) =>
        ToVector3(pawn.AbsOrigin!) + new Vector3(0, 0, pawn.ViewOffset.Z - 2f);

    private static void ApplySpeed(int slot)
    {
        var pawn = Utilities.GetPlayerFromSlot(slot)?.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid) return;
        pawn.VelocityModifier = SpeedMultiplier;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }

    private static void ResetSpeed(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn is null || !pawn.IsValid) return;
        pawn.VelocityModifier = 1f;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_flVelocityModifier");
    }

    // Bunnyhopping is a server setting, so it is on for everybody as long as one player wants it.
    private void ApplyBhop()
    {
        string value = _cheats.AnyoneHas(CheatKind.Bhop) ? "1" : "0";
        Server.ExecuteCommand($"sv_autobunnyhopping {value}");
        Server.ExecuteCommand($"sv_enablebunnyhopping {value}");
    }

    // ---------- ESP: glowing copies of enemies, only sent to the cheater ----------

    private void RefreshGlows()
    {
        if (!_testMode()) return;
        foreach (int slot in _cheats.SlotsWith(CheatKind.Esp).ToList())
        {
            var cheater = Utilities.GetPlayerFromSlot(slot);
            if (cheater is null || !cheater.IsValid) continue;
            var enemies = Utilities.GetPlayers()
                .Where(p => p.IsValid && p.TeamNum >= 2 && p.TeamNum != cheater.TeamNum)
                .Select(p => p.PlayerPawn.Value)
                .Where(p => p is not null && p.IsValid && p.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                .Cast<CCSPlayerPawn>()
                .ToList();
            _glows.Sync(slot, enemies, cheater.TeamNum == 2 ? Color.FromArgb(255, 80, 160, 255) : Color.FromArgb(255, 255, 80, 60));
        }
    }

    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
        {
            if (player is null || !player.IsValid) continue;

            // Remember which enemy pawns this player receives.
            if (_cheats.IsOn(player.Slot, CheatKind.Radar) || _cheats.IsOn(player.Slot, CheatKind.AimLegit) ||
                _cheats.IsOn(player.Slot, CheatKind.AimRage) || _cheats.IsOn(player.Slot, CheatKind.Esp))
            {
                var sent = new HashSet<uint>();
                foreach (var other in Utilities.GetPlayers())
                {
                    var pawn = other.PlayerPawn.Value;
                    if (pawn is not null && pawn.IsValid && info.TransmitEntities.Contains(pawn.Index)) sent.Add(pawn.Index);
                }
                _transmittedTo[player.Slot] = sent;
            }

            // Glow copies: only the owner sees them, and only for enemies his client receives.
            foreach (var glow in _glows.All)
            {
                bool show = glow.OwnerSlot == player.Slot && info.TransmitEntities.Contains(glow.TargetIndex);
                if (!show)
                {
                    info.TransmitEntities.Remove(glow.GlowIndex);
                    info.TransmitEntities.Remove(glow.RelayIndex);
                }
            }
        }
    }

    private static Vector3 ToVector3(Vector v) => new(v.X, v.Y, v.Z);
}
