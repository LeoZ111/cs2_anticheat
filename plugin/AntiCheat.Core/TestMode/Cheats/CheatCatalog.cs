namespace AntiCheat.Core.TestMode.Cheats;

/// <summary>
/// Simulated cheats for the test server. They run on the server, never on a game client,
/// and only use what the cheater's client would really receive.
/// </summary>
public enum CheatKind
{
    Radar,
    Esp,
    AimLegit,
    AimRage,
    Speed,
    Bhop,
}

public sealed record CheatInfo(CheatKind Kind, string Name, string Description);

public static class CheatCatalog
{
    public static readonly IReadOnlyList<CheatInfo> All = new[]
    {
        new CheatInfo(CheatKind.Radar, "radar", "Radar-Hack: alle Gegner auf dem Radar"),
        new CheatInfo(CheatKind.Esp, "esp", "ESP/Wallhack: Gegner leuchten durch Wände"),
        new CheatInfo(CheatKind.AimLegit, "aim_legit", "Aimbot legit: zieht sanft zum Kopf, kleines Sichtfeld"),
        new CheatInfo(CheatKind.AimRage, "aim_rage", "Aimbot rage: schnappt sofort auf jeden Kopf"),
        new CheatInfo(CheatKind.Speed, "speed", "Speedhack: 50 % schneller laufen"),
        new CheatInfo(CheatKind.Bhop, "bhop", "Bunnyhop: Springen halten (gilt für alle auf dem Server)"),
    };

    public static CheatInfo? Find(string name)
    {
        return All.FirstOrDefault(c => string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>"on" / "off" / "" (toggle). Returns null for anything else.</summary>
    public static bool? ParseSwitch(string text, bool current)
    {
        return text.Trim().ToLowerInvariant() switch
        {
            "" => !current,
            "on" or "an" or "1" => true,
            "off" or "aus" or "0" => false,
            _ => null,
        };
    }
}

/// <summary>Which cheats each player has switched on. Key: player slot.</summary>
public sealed class PlayerCheats
{
    private readonly Dictionary<int, HashSet<CheatKind>> _active = new();

    public bool IsOn(int slot, CheatKind kind) => _active.TryGetValue(slot, out var set) && set.Contains(kind);

    public void Set(int slot, CheatKind kind, bool on)
    {
        if (!_active.TryGetValue(slot, out var set))
        {
            set = new HashSet<CheatKind>();
            _active[slot] = set;
        }
        if (on)
        {
            set.Add(kind);
            // Only one aimbot at a time.
            if (kind == CheatKind.AimLegit) set.Remove(CheatKind.AimRage);
            if (kind == CheatKind.AimRage) set.Remove(CheatKind.AimLegit);
        }
        else
        {
            set.Remove(kind);
        }
    }

    public IEnumerable<int> SlotsWith(CheatKind kind) => _active.Where(p => p.Value.Contains(kind)).Select(p => p.Key);

    public bool AnyoneHas(CheatKind kind) => SlotsWith(kind).Any();

    public void Clear(int slot) => _active.Remove(slot);
}
