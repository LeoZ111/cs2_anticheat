using AntiCheat.Core.TestMode;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;

namespace AntiCheat.CounterStrikeSharp.TestMode;

/// <summary>
/// Chat command for the test server: !enemy_skill noob|mid|hard|pro|legend
/// (or /enemy_skill ... to hide the command from chat).
/// </summary>
public static class BotSkillCommand
{
    public static void Register(BasePlugin plugin)
    {
        plugin.AddCommand("css_enemy_skill", "Set bot skill: " + BotSkill.AllNames, OnCommand);
    }

    private static void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        BotSkillLevel? level = BotSkill.Parse(command.ArgCount > 1 ? command.GetArg(1) : null);
        if (level is null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Benutzung: !enemy_skill <{BotSkill.AllNames}>");
            return;
        }

        BotSkillSettings settings = BotSkill.SettingsFor(level.Value);

        // bot_difficulty only applies to newly added bots, so we replace all bots.
        int quota = ConVar.Find("bot_quota")?.GetPrimitiveValue<int>() ?? 0;
        Server.ExecuteCommand($"bot_difficulty {settings.BotDifficulty}");
        Server.ExecuteCommand("bot_kick");
        Server.ExecuteCommand($"bot_quota {Math.Max(quota, 1)}");

        string name = level.Value.ToString().ToUpperInvariant();
        string note = settings.UsesOwnBotControl ? " (vorerst wie PRO, eigene Bot-Steuerung kommt noch)" : "";
        Server.PrintToChatAll($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} Bot-Stärke: {ChatColors.Gold}{name}{ChatColors.Default}{note}");
    }
}
