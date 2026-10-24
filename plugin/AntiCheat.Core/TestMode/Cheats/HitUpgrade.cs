namespace AntiCheat.Core.TestMode.Cheats;

/// <summary>CS2 hit groups (same numbers as the game's HitGroup_t).</summary>
public enum HitGroup
{
    Generic = 0,
    Head = 1,
    Chest = 2,
    Stomach = 3,
    LeftArm = 4,
    RightArm = 5,
    LeftLeg = 6,
    RightLeg = 7,
    Neck = 8,
    Gear = 10,
}

public readonly record struct Hit(float Damage, HitGroup Group);

/// <summary>
/// Simulated aimbot, part "hit upgrade": a real hit becomes a better hit.
/// Rage: every body hit becomes a headshot. Legit: body hits get a bit stronger, like a slightly better aim.
/// Only real hits are changed; misses stay misses (that would need a client-side cheat).
/// </summary>
public static class HitUpgrade
{
    public const float LegitBonus = 1.5f;

    /// <summary>How much damage a hit group does compared to a chest hit (CS2 defaults).</summary>
    public static float Multiplier(HitGroup group) => group switch
    {
        HitGroup.Head => 4.0f,
        HitGroup.Stomach => 1.25f,
        HitGroup.LeftLeg or HitGroup.RightLeg => 0.75f,
        _ => 1.0f,
    };

    public static Hit Apply(CheatKind aimbot, Hit hit)
    {
        switch (aimbot)
        {
            case CheatKind.AimRage when hit.Group != HitGroup.Head:
                float baseDamage = hit.Damage / Multiplier(hit.Group);
                return new Hit(baseDamage * Multiplier(HitGroup.Head), HitGroup.Head);
            case CheatKind.AimLegit when hit.Group != HitGroup.Head:
                return hit with { Damage = hit.Damage * LegitBonus };
            default:
                return hit;
        }
    }
}
