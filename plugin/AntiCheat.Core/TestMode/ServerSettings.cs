using System.Globalization;

namespace AntiCheat.Core.TestMode;

/// <summary>
/// Turns chat input like "mirage", "max" or "2.5" into the server commands for the test server.
/// Every method returns null if the input is not valid, so the chat command can show the options.
/// </summary>
public static class ServerSettings
{
    public static readonly IReadOnlyList<string> Maps = new[]
    {
        "de_ancient", "de_anubis", "de_dust2", "de_inferno", "de_mirage", "de_nuke", "de_overpass", "de_train", "de_vertigo",
        "cs_italy", "cs_office",
    };

    public static readonly IReadOnlyList<string> NadeModes = new[] { "off", "less", "normal", "more", "max" };

    // Short name in chat -> what bots get (bot_loadout). Rifles and SMGs come with a Tec-9 as pistol.
    private static readonly Dictionary<string, string> BotWeapons = new()
    {
        ["deagle"] = "deagle", ["p250"] = "p250", ["fn57"] = "fn57", ["cz75a"] = "cz75a", ["elite"] = "elite", ["r8"] = "revolver",
        ["mp9"] = "mp9 tec9", ["mac10"] = "mac10 tec9", ["mp7"] = "mp7 tec9", ["mp5sd"] = "mp5sd tec9", ["ump45"] = "ump45 tec9",
        ["p90"] = "p90 tec9", ["bizon"] = "bizon tec9",
        ["nova"] = "nova tec9", ["xm1014"] = "xm1014 tec9", ["mag7"] = "mag7 tec9", ["sawedoff"] = "sawedoff tec9",
        ["famas"] = "famas tec9", ["galilar"] = "galilar tec9", ["m4a1"] = "m4a1 tec9", ["m4a1s"] = "m4a1_silencer tec9",
        ["ak47"] = "ak47 tec9", ["aug"] = "aug tec9", ["sg556"] = "sg556 tec9",
        ["ssg08"] = "ssg08 tec9", ["awp"] = "awp tec9", ["scar20"] = "scar20 tec9", ["g3sg1"] = "g3sg1 tec9",
        ["negev"] = "negev tec9", ["m249"] = "m249 tec9",
    };

    public static IReadOnlyList<string> BotWeaponNames => BotWeapons.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    /// <summary>"mirage" or "de_mirage" -> "de_mirage".</summary>
    public static string? ResolveMap(string input)
    {
        string name = input.Trim().ToLowerInvariant();
        return Maps.FirstOrDefault(m => m == name || m[3..] == name);
    }

    /// <summary>Money: "max" (16000 and buy anywhere), "normal" (competitive default) or an amount from 0 to 65535.</summary>
    public static MoneySettings? ParseMoney(string input)
    {
        string text = input.Trim().ToLowerInvariant();
        if (text == "max")
        {
            return new MoneySettings(16000, 16000, 16000, BuyAnywhere: true, BuyTimeSeconds: 9999);
        }
        if (text == "normal")
        {
            return new MoneySettings(800, 16000, 0, BuyAnywhere: false, BuyTimeSeconds: 20);
        }
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int amount) && amount <= 65535)
        {
            return new MoneySettings(amount, Math.Max(amount, 16000), amount, BuyAnywhere: false, BuyTimeSeconds: 20);
        }
        return null;
    }

    /// <summary>Round time in minutes, e.g. "2" or "1,5" or "1.5". Allowed: 0.5 to 60.</summary>
    public static IReadOnlyList<string>? RoundTime(string input)
    {
        string text = input.Trim().Replace(',', '.');
        if (!double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double minutes)
            || minutes < 0.5 || minutes > 60)
        {
            return null;
        }
        string value = minutes.ToString("0.##", CultureInfo.InvariantCulture);
        return new[] { $"mp_roundtime {value}", $"mp_roundtime_defuse {value}", $"mp_roundtime_hostage {value}" };
    }

    /// <summary>"on" restarts the warmup, "off" ends it.</summary>
    public static IReadOnlyList<string>? Warmup(string input)
    {
        return input.Trim().ToLowerInvariant() switch
        {
            "off" => new[] { "mp_warmup_end" },
            "on" => new[] { "mp_warmup_start" },
            _ => null,
        };
    }

    /// <summary>How many grenades the bots throw (needs NadeSystem from CS2-Bot-Improver).</summary>
    public static IReadOnlyList<string>? Nades(string input)
    {
        string mode = input.Trim().ToLowerInvariant();
        return NadeModes.Contains(mode) ? new[] { $"bot_nades {mode}" } : null;
    }

    /// <summary>
    /// The value for bot_loadout: what every bot gets from the next round on.
    /// "" means normal buying again; null means unknown weapon.
    /// </summary>
    public static string? BotLoadout(string input)
    {
        string name = input.Trim().ToLowerInvariant();
        if (name == "normal")
        {
            return "";
        }
        return BotWeapons.TryGetValue(name, out string? loadout) ? $"{loadout} vesthelm defuser" : null;
    }
}

public sealed record MoneySettings(int StartMoney, int MaxMoney, int AfterRoundMoney, bool BuyAnywhere, int BuyTimeSeconds)
{
    public IReadOnlyList<string> Commands => new[]
    {
        $"mp_startmoney {StartMoney}",
        $"mp_maxmoney {MaxMoney}",
        $"mp_afterroundmoney {AfterRoundMoney}",
        $"mp_buy_anywhere {(BuyAnywhere ? 1 : 0)}",
        $"mp_buytime {BuyTimeSeconds}",
    };
}
