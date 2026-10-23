using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;

namespace AntiCheat.CounterStrikeSharp.TestMode;

/// <summary>
/// Server console command for debugging: css_ac_bots prints where every bot is.
/// Run it twice a few seconds apart to see if the bots move.
/// </summary>
public static class BotPositionsCommand
{
    public static void Register(BasePlugin plugin)
    {
        plugin.AddCommand("css_ac_bots", "Print bot positions (debug)", OnCommand);
    }

    private static void OnCommand(CCSPlayerController? player, CommandInfo command)
    {
        var rules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault()?.GameRules;
        if (rules is not null)
        {
            command.ReplyToCommand($"[AntiCheat] warmup {rules.WarmupPeriod} freeze {rules.FreezePeriod} " +
                $"paused {rules.MatchWaitingForResume} phase {rules.GamePhase} roundtime {rules.RoundTime} " +
                $"time {Server.CurrentTime:F1} tick {Server.TickCount}");
        }

        var bots = Utilities.GetPlayers().Where(p => p.IsBot).ToList();
        command.ReplyToCommand($"[AntiCheat] {bots.Count} bots");
        foreach (var bot in bots)
        {
            var pawn = bot.PlayerPawn.Value;
            var origin = pawn?.AbsOrigin;
            string position = origin is null ? "no pawn" : $"{origin.X:F0} {origin.Y:F0} {origin.Z:F0}";
            command.ReplyToCommand($"  {bot.PlayerName} team {bot.TeamNum} alive {pawn?.LifeState == 0} pos {position}");
        }
    }
}
