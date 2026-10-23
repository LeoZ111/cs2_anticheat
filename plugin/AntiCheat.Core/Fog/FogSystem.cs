using System.Numerics;
using AntiCheat.Core.Geometry;

namespace AntiCheat.Core.Fog;

/// <summary>Everything the fog needs to know about one player in one tick.</summary>
/// <param name="Origin">Feet position.</param>
/// <param name="EyeHeight">Eye height above the feet (64 standing, about 46 crouched).</param>
/// <param name="PingSeconds">The player's latency; used to send enemies a bit early.</param>
/// <param name="LoudUntilTick">The player made a loud sound (shot) and stays audible until this tick.</param>
public readonly record struct FogPlayer(int Slot, int Team, bool Alive, Vector3 Origin, float EyeHeight, Vector3 Velocity,
    float PingSeconds, int LoudUntilTick);

/// <summary>Why an enemy was sent to a viewer (or not). Used for statistics and leak measurement.</summary>
public enum FogReason
{
    Hidden,
    Visible,
    Holding,
    Audible,
    Near,
    NotAnEnemy,
}

public sealed record FogSettings
{
    /// <summary>Extra time added to the ping when predicting movement (seconds).</summary>
    public float LookaheadExtra { get; init; } = 0.1f;

    /// <summary>After an enemy was last visible, keep sending him for this many ticks (against flickering).</summary>
    public int HoldTicks { get; init; } = 8;

    /// <summary>Enemies closer than this are always sent (collision, knife fights).</summary>
    public float AlwaysSendRadius { get; init; } = 160f;

    /// <summary>Running enemies within this distance can be heard, so they are sent.</summary>
    public float FootstepRadius { get; init; } = 1100f;

    /// <summary>Speed above which footsteps make noise (walking is silent).</summary>
    public float RunSpeed { get; init; } = 135f;

    /// <summary>Shooting enemies within this distance can be heard.</summary>
    public float GunshotRadius { get; init; } = 2500f;

    /// <summary>Half the width of a player body, for the shoulder points.</summary>
    public float ShoulderOffset { get; init; } = 14f;
}

/// <summary>
/// The knowledge layer of the fog of war: for every viewer and every enemy, should the server send the enemy?
/// Rule of thumb from the concept: if in doubt, send (and measure the leak).
/// </summary>
public sealed class FogSystem
{
    private readonly Bvh _map;
    private readonly FogSettings _settings;
    private readonly Dictionary<(int Viewer, int Target), int> _lastVisibleTick = new();
    private readonly Dictionary<(int Viewer, int Target), FogReason> _decision = new();

    public FogSystem(Bvh map, FogSettings? settings = null)
    {
        _map = map;
        _settings = settings ?? new FogSettings();
    }

    /// <summary>Number of sight lines tested in the last update (for performance stats).</summary>
    public int LinesLastUpdate { get; private set; }

    public IReadOnlyDictionary<(int Viewer, int Target), FogReason> Decisions => _decision;

    public void Update(IReadOnlyList<FogPlayer> players, int tick)
    {
        _decision.Clear();
        LinesLastUpdate = 0;
        foreach (var viewer in players)
        {
            foreach (var target in players)
            {
                if (viewer.Slot == target.Slot) continue;
                _decision[(viewer.Slot, target.Slot)] = Decide(viewer, target, tick);
            }
        }
    }

    /// <summary>Should the server send target to viewer? Unknown pairs are sent (if in doubt, send).</summary>
    public bool ShouldSend(int viewerSlot, int targetSlot) =>
        !_decision.TryGetValue((viewerSlot, targetSlot), out var reason) || reason != FogReason.Hidden;

    public void Forget(int slot)
    {
        foreach (var key in _lastVisibleTick.Keys.Where(k => k.Viewer == slot || k.Target == slot).ToList())
        {
            _lastVisibleTick.Remove(key);
        }
    }

    private FogReason Decide(FogPlayer viewer, FogPlayer target, int tick)
    {
        // Teammates, dead players (spectating) and dead targets: no secret to keep.
        if (viewer.Team == target.Team || viewer.Team < 2 || target.Team < 2 || !viewer.Alive || !target.Alive)
        {
            return FogReason.NotAnEnemy;
        }

        float distance = Vector3.Distance(viewer.Origin, target.Origin);
        if (distance < _settings.AlwaysSendRadius) return FogReason.Near;

        if (CanSee(viewer, target))
        {
            _lastVisibleTick[(viewer.Slot, target.Slot)] = tick;
            return FogReason.Visible;
        }

        if (_lastVisibleTick.TryGetValue((viewer.Slot, target.Slot), out int last) && tick - last <= _settings.HoldTicks)
        {
            return FogReason.Holding;
        }

        if (IsAudible(target, distance, tick)) return FogReason.Audible;

        return FogReason.Hidden;
    }

    private bool IsAudible(FogPlayer target, float distance, int tick)
    {
        bool running = new Vector2(target.Velocity.X, target.Velocity.Y).Length() > _settings.RunSpeed;
        if (running && distance < _settings.FootstepRadius) return true;
        return tick <= target.LoudUntilTick && distance < _settings.GunshotRadius;
    }

    private bool CanSee(FogPlayer viewer, FogPlayer target)
    {
        float lookahead = viewer.PingSeconds + target.PingSeconds + _settings.LookaheadExtra;
        Vector3 viewerEye = viewer.Origin + new Vector3(0, 0, viewer.EyeHeight);
        Span<Vector3> eyes = stackalloc Vector3[2];
        eyes[0] = viewerEye;
        eyes[1] = viewerEye + Horizontal(viewer.Velocity) * lookahead; // where the viewer may be by the time he sees it

        Span<Vector3> bodies = stackalloc Vector3[2];
        bodies[0] = target.Origin;
        bodies[1] = target.Origin + Horizontal(target.Velocity) * lookahead;

        foreach (Vector3 eye in eyes)
        {
            foreach (Vector3 body in bodies)
            {
                foreach (Vector3 point in BodyPoints(eye, body, target.EyeHeight))
                {
                    LinesLastUpdate++;
                    if (!_map.IsBlocked(new Segment(eye, point))) return true;
                }
            }
        }
        return false;
    }

    /// <summary>Head, chest, near the feet, and both shoulders (seen from the viewer).</summary>
    public IEnumerable<Vector3> BodyPoints(Vector3 eye, Vector3 feet, float eyeHeight)
    {
        Vector3 toTarget = Horizontal(feet - eye);
        Vector3 side = toTarget.LengthSquared() > 1e-4f
            ? Vector3.Normalize(new Vector3(-toTarget.Y, toTarget.X, 0)) * _settings.ShoulderOffset
            : new Vector3(_settings.ShoulderOffset, 0, 0);
        Vector3 chest = feet + new Vector3(0, 0, eyeHeight * 0.75f);
        yield return feet + new Vector3(0, 0, eyeHeight);   // head
        yield return chest;
        yield return feet + new Vector3(0, 0, 10f);          // just above the ground
        yield return chest + side;
        yield return chest - side;
    }

    private static Vector3 Horizontal(Vector3 v) => new(v.X, v.Y, 0);
}
