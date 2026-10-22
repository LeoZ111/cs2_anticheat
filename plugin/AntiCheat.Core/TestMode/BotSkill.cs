namespace AntiCheat.Core.TestMode;

/// <summary>How strong the bots on the test server play.</summary>
public enum BotSkillLevel
{
    Noob,
    Mid,
    Hard,
    Pro,
    Legend,
}

/// <summary>What the server has to set for one skill level.</summary>
/// <param name="BotDifficulty">CS2's own bot_difficulty: 0 easy, 1 normal, 2 hard, 3 expert.</param>
/// <param name="UsesOwnBotControl">True once our own bot steering (aim, movement, sounds) takes over.</param>
public readonly record struct BotSkillSettings(int BotDifficulty, bool UsesOwnBotControl);

public static class BotSkill
{
    /// <summary>Reads a level from chat text like "hard" or "PRO". Returns null if unknown.</summary>
    public static BotSkillLevel? Parse(string? text)
    {
        return text?.Trim().ToLowerInvariant() switch
        {
            "noob" => BotSkillLevel.Noob,
            "mid" => BotSkillLevel.Mid,
            "hard" => BotSkillLevel.Hard,
            "pro" or "expert" => BotSkillLevel.Pro,
            "legend" => BotSkillLevel.Legend,
            _ => null,
        };
    }

    public static BotSkillSettings SettingsFor(BotSkillLevel level)
    {
        return level switch
        {
            BotSkillLevel.Noob => new BotSkillSettings(0, false),
            BotSkillLevel.Mid => new BotSkillSettings(1, false),
            BotSkillLevel.Hard => new BotSkillSettings(2, false),
            BotSkillLevel.Pro => new BotSkillSettings(3, false),
            // CS2 has no level above expert. Legend gets better than Pro with our own bot steering (later).
            BotSkillLevel.Legend => new BotSkillSettings(3, true),
            _ => throw new ArgumentOutOfRangeException(nameof(level)),
        };
    }

    /// <summary>All level names, for the help text in chat.</summary>
    public static string AllNames => string.Join(", ", Enum.GetNames<BotSkillLevel>().Select(n => n.ToLowerInvariant()));
}
