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
            if (BotSkill.SettingsFor(level).UsesProTeam)
            {
                label += " (Gegner: Pro-Team)";
            }
            menu.AddMenuOption(label, (_, _) => Apply(plugin, chosen));
        }
        MenuManager.OpenChatMenu(player, menu);
    }

    private static void Apply(BasePlugin plugin, BotSkillLevel level)
    {
        BotSkillSettings settings = BotSkill.SettingsFor(level);
        Server.ExecuteCommand($"bot_difficulty {settings.BotDifficulty}");
        var bots = Utilities.GetPlayers().Where(p => p.IsBot && p.UserId.HasValue).ToList();

        if (settings.UsesProTeam)
        {
            BringInProTeam(plugin, bots);
            return;
        }

        string name = level.ToString().ToUpperInvariant();
        Server.PrintToChatAll($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} Bot-Stärke: {ChatColors.Gold}{name}{ChatColors.Default}. Bots werden nacheinander ersetzt.");

        // bot_difficulty only applies to newly added bots. kickid keeps bot_quota as it is,
        // so the server fills each free slot again with a bot of the new difficulty.
        for (int i = 0; i < bots.Count; i++)
        {
            int userId = bots[i].UserId!.Value;
            plugin.AddTimer(i * SecondsBetweenBots, () => Server.ExecuteCommand($"kickid {userId}"));
        }
        plugin.Logger.LogInformation("bot_difficulty {Difficulty}, replacing {Count} bots", settings.BotDifficulty, bots.Count);
    }

    // Legend: the enemy bots are replaced by bot profiles of a real pro team.
    private static void BringInProTeam(BasePlugin plugin, List<CCSPlayerController> bots)
    {
        var humanTeams = Utilities.GetPlayers().Where(p => !p.IsBot && !p.IsHLTV).Select(p => (int)p.TeamNum);
        int enemySide = ProTeams.EnemySideOf(humanTeams);
        string side = enemySide == ProTeams.Terrorists ? "t" : "ct";
        ProTeam team = ProTeams.Pick(Random.Shared);
        var enemyBotIds = bots.Where(b => b.TeamNum == enemySide).Select(b => b.UserId!.Value).ToList();

        Server.PrintToChatAll($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} Bot-Stärke: {ChatColors.Gold}LEGEND{ChatColors.Default}. Gegner: {ChatColors.Gold}{team.Name}{ChatColors.Default} ({string.Join(", ", team.Players)})");

        // bot_kick lowers bot_quota and bot_add raises it again, so the quota does not fill in random bots in between.
        int steps = Math.Max(enemyBotIds.Count, team.Players.Count);
        for (int i = 0; i < steps; i++)
        {
            float time = i * SecondsBetweenBots;
            if (i < enemyBotIds.Count)
            {
                int userId = enemyBotIds[i];
                plugin.AddTimer(time, () => KickBotLoweringQuota(userId));
            }
            if (i < team.Players.Count)
            {
                string pro = team.Players[i];
                plugin.AddTimer(time + SecondsBetweenBots / 2, () => Server.ExecuteCommand($"bot_add_{side} \"{pro}\""));
            }
        }
        plugin.Logger.LogInformation("legend: {Team} on {Side}, replacing {Count} enemy bots", team.Name, side, enemyBotIds.Count);
    }

    private static void KickBotLoweringQuota(int userId)
    {
        var bot = Utilities.GetPlayerFromUserid(userId);
        if (bot is { IsValid: true, IsBot: true })
        {
            Server.ExecuteCommand($"bot_kick \"{bot.PlayerName}\"");
        }
    }
}
