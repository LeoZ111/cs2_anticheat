using AntiCheat.Core.TestMode;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace AntiCheat.CounterStrikeSharp.TestMode;

/// <summary>
/// Chat command for the test server:
///   !bot_skill          opens a menu with all levels (choose with !1 ... !5)
///   !bot_skill pro      sets the level directly
/// Use / instead of ! to hide the command from chat.
/// </summary>
public static class BotSkillCommand
{
    // Swapping all bots in the same tick overflows the clients' network channel and can freeze bots,
    // so we swap them one by one.
    private const float SecondsBetweenBots = 0.5f;

    public static void Register(BasePlugin plugin)
    {
        plugin.AddCommand("css_bot_skill", "Set bot skill: " + BotSkill.AllNames,
            (player, command) => OnCommand(plugin, player, command));
    }

    private static void OnCommand(BasePlugin plugin, CCSPlayerController? player, CommandInfo command)
    {
        string argument = command.ArgCount > 1 ? command.GetArg(1) : "";
        plugin.Logger.LogInformation("bot_skill '{Argument}' from {Player}", argument, player?.PlayerName ?? "console");

        if (argument.Length == 0 && player is not null)
        {
            OpenMenu(plugin, player);
            return;
        }

        BotSkillLevel? level = BotSkill.Parse(argument);
        if (level is null)
        {
            command.ReplyToCommand($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Unbekannte Stufe. Tippe !bot_skill für das Menü ({BotSkill.AllNames}).");
            return;
        }

        Apply(plugin, level.Value);
    }

    private static void OpenMenu(BasePlugin plugin, CCSPlayerController player)
    {
        var menu = new ChatMenu($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} Bot-Stärke wählen:");
        foreach (BotSkillLevel level in Enum.GetValues<BotSkillLevel>())
        {
            BotSkillLevel chosen = level;
            string label = level.ToString().ToLowerInvariant();
            if (BotSkill.SettingsFor(level).UsesOwnBotControl)
            {
                label += " (vorerst wie pro)";
            }
            menu.AddMenuOption(label, (_, _) => Apply(plugin, chosen));
        }
        MenuManager.OpenChatMenu(player, menu);
    }

    private static void Apply(BasePlugin plugin, BotSkillLevel level)
    {
        BotSkillSettings settings = BotSkill.SettingsFor(level);
        string name = level.ToString().ToUpperInvariant();
        string note = settings.UsesOwnBotControl ? " (vorerst wie PRO, eigene Bot-Steuerung kommt noch)" : "";
        Server.PrintToChatAll($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} Bot-Stärke: {ChatColors.Gold}{name}{ChatColors.Default}{note}. Bots werden nacheinander ersetzt.");

        // bot_difficulty only applies to newly added bots. We kick the bots one by one;
        // bot_quota fills the free slots again with bots of the new difficulty.
        Server.ExecuteCommand($"bot_difficulty {settings.BotDifficulty}");
        var botUserIds = Utilities.GetPlayers().Where(p => p.IsBot && p.UserId.HasValue).Select(p => p.UserId!.Value).ToList();
        for (int i = 0; i < botUserIds.Count; i++)
        {
            int userId = botUserIds[i];
            plugin.AddTimer(i * SecondsBetweenBots, () => Server.ExecuteCommand($"kickid {userId}"));
        }
        plugin.Logger.LogInformation("bot_difficulty {Difficulty}, replacing {Count} bots", settings.BotDifficulty, botUserIds.Count);
    }
}
