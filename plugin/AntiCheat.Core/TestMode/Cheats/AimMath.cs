using System.Numerics;

namespace AntiCheat.Core.TestMode.Cheats;

/// <summary>View direction like in CS2: pitch positive = looking down, yaw 0 = along +X, 90 = along +Y. Degrees.</summary>
public readonly record struct ViewAngles(float Pitch, float Yaw);

/// <summary>How an aimbot behaves.</summary>
/// <param name="FieldOfView">Only targets within this many degrees of the crosshair.</param>
/// <param name="Smoothing">Share of the remaining angle closed per tick (1 = instant snap).</param>
public readonly record struct AimProfile(float FieldOfView, float Smoothing)
{
    public static readonly AimProfile Legit = new(12f, 0.12f);
    public static readonly AimProfile Rage = new(180f, 1f);
}

public readonly record struct AimTarget(int Id, Vector3 Point);

public static class AimMath
{
    private const float RadToDeg = 180f / MathF.PI;

    public static ViewAngles AnglesTo(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        float horizontal = MathF.Sqrt(d.X * d.X + d.Y * d.Y);
        return new ViewAngles(-MathF.Atan2(d.Z, horizontal) * RadToDeg, MathF.Atan2(d.Y, d.X) * RadToDeg);
    }

    /// <summary>Difference between two angles, from -180 to 180.</summary>
    public static float Delta(float from, float to)
    {
        float d = (to - from) % 360f;
        if (d > 180f) d -= 360f;
        if (d < -180f) d += 360f;
        return d;
    }

    /// <summary>How far (in degrees) the crosshair is from pointing at the target.</summary>
    public static float Distance(ViewAngles current, ViewAngles target)
    {
        float dp = Delta(current.Pitch, target.Pitch);
        float dy = Delta(current.Yaw, target.Yaw);
        return MathF.Sqrt(dp * dp + dy * dy);
    }

    /// <summary>One tick of moving the crosshair towards the target.</summary>
    public static ViewAngles Step(ViewAngles current, ViewAngles target, float smoothing)
    {
        float s = Math.Clamp(smoothing, 0f, 1f);
        float pitch = current.Pitch + Delta(current.Pitch, target.Pitch) * s;
        float yaw = current.Yaw + Delta(current.Yaw, target.Yaw) * s;
        return new ViewAngles(Math.Clamp(pitch, -89f, 89f), Normalize(yaw));
    }

    public static float Normalize(float yaw)
    {
        float y = yaw % 360f;
        if (y > 180f) y -= 360f;
        if (y < -180f) y += 360f;
        return y;
    }

    /// <summary>
    /// Is the crosshair on the enemy's body? About 16 units to each side of the aim point,
    /// so the allowed angle shrinks with distance (never below the minimum).
    /// </summary>
    public static bool IsOnBody(float errorDegrees, float distance, float halfWidth = 16f, float minimum = 1.0f)
    {
        float allowed = MathF.Max(minimum, MathF.Atan2(halfWidth, MathF.Max(distance, 1f)) * RadToDeg);
        return errorDegrees <= allowed;
    }

    /// <summary>The target closest to the crosshair inside the field of view, or null.</summary>
    public static AimTarget? PickTarget(Vector3 eye, ViewAngles current, IEnumerable<AimTarget> targets, float fieldOfView)
    {
        AimTarget? best = null;
        float bestDistance = fieldOfView;
        foreach (var target in targets)
        {
            float distance = Distance(current, AnglesTo(eye, target.Point));
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = target;
            }
        }
        return best;
    }
}
