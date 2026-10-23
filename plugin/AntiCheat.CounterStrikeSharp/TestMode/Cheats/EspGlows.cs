using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace AntiCheat.CounterStrikeSharp.TestMode.Cheats;

/// <summary>
/// Glowing copies of enemy player models for the ESP. Each glow is an invisible "relay" prop that follows the enemy,
/// plus a glowing prop that follows the relay. CheckTransmit hides both from everyone except the cheater.
/// </summary>
public sealed class EspGlows
{
    public sealed record Glow(int OwnerSlot, uint TargetIndex, uint RelayIndex, uint GlowIndex, CBaseModelEntity Relay, CBaseModelEntity Model);

    private readonly List<Glow> _glows = new();

    public IReadOnlyList<Glow> All => _glows;

    /// <summary>Makes sure the cheater has exactly one glow per living enemy.</summary>
    public void Sync(int ownerSlot, IReadOnlyList<CCSPlayerPawn> enemies, Color color)
    {
        var wanted = enemies.Select(e => e.Index).ToHashSet();
        foreach (var glow in _glows.Where(g => g.OwnerSlot == ownerSlot && (!wanted.Contains(g.TargetIndex) || !g.Model.IsValid)).ToList())
        {
            Remove(glow);
        }
        foreach (var enemy in enemies)
        {
            if (_glows.Any(g => g.OwnerSlot == ownerSlot && g.TargetIndex == enemy.Index)) continue;
            var glow = Create(ownerSlot, enemy, color);
            if (glow is not null) _glows.Add(glow);
        }
    }

    public void RemoveFor(int ownerSlot)
    {
        foreach (var glow in _glows.Where(g => g.OwnerSlot == ownerSlot).ToList()) Remove(glow);
    }

    public void RemoveAll()
    {
        foreach (var glow in _glows.ToList()) Remove(glow);
    }

    private void Remove(Glow glow)
    {
        if (glow.Model.IsValid) glow.Model.Remove();
        if (glow.Relay.IsValid) glow.Relay.Remove();
        _glows.Remove(glow);
    }

    private static Glow? Create(int ownerSlot, CCSPlayerPawn enemy, Color color)
    {
        string? model = enemy.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName;
        if (string.IsNullOrEmpty(model)) return null;

        var relay = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        var glow = Utilities.CreateEntityByName<CBaseModelEntity>("prop_dynamic");
        if (relay is null || glow is null) return null;

        relay.SetModel(model);
        relay.Spawnflags = 256u; // no physics/collision
        relay.RenderMode = RenderMode_t.kRenderNone;
        relay.DispatchSpawn();

        glow.SetModel(model);
        glow.Spawnflags = 256u;
        glow.DispatchSpawn();
        glow.Glow.GlowColorOverride = color;
        glow.Glow.GlowRange = 5000;
        glow.Glow.GlowRangeMin = 0;
        glow.Glow.GlowTeam = -1;
        glow.Glow.GlowType = 3; // always visible, also through walls

        relay.AcceptInput("FollowEntity", enemy, relay, "!activator");
        glow.AcceptInput("FollowEntity", relay, glow, "!activator");

        return new Glow(ownerSlot, enemy.Index, relay.Index, glow.Index, relay, glow);
    }
}
