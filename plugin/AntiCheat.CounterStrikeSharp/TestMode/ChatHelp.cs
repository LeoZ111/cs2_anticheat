using AntiCheat.Core.TestMode;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace AntiCheat.CounterStrikeSharp.TestMode;

/// <summary>
/// Chat help, as close to tab completion as CS2 allows (the server only sees a message after Enter):
///   !          lists all commands alphabetically
///   !b         lists all commands starting with "b"
///   !bot_s pro runs !bot_skill pro, because only one command starts with "bot_s"
/// </summary>
public static class ChatHelp
{
    private const string HelpName = "help";

    public static void Register(BasePlugin plugin, IReadOnlyList<ChatCommandInfo> commands)
    {
        var all = commands.Append(new ChatCommandInfo(HelpName, "", "Zeigt diese Liste")).ToList();
        CommandInfo.CommandListenerCallback onSay = (player, info) => OnSay(player, info, all);
        plugin.AddCommandListener("say", onSay, HookMode.Pre);
        plugin.AddCommandListener("say_team", onSay, HookMode.Pre);
    }

    private static HookResult OnSay(CCSPlayerController? player, CommandInfo info, List<ChatCommandInfo> all)
    {
        if (player is null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        var parts = ChatCommandCatalog.Split(info.ArgString);
        if (parts is null)
        {
            return HookResult.Continue;
        }
        (string name, string rest) = parts.Value;
        bool silent = info.ArgString.TrimStart('"', ' ').StartsWith('/');
        HookResult done = silent ? HookResult.Handled : HookResult.Continue;

        // Real commands are handled by CounterStrikeSharp itself.
        var exact = ChatCommandCatalog.Exact(all, name);
        if (exact is not null && exact.Name != HelpName)
        {
            return HookResult.Continue;
        }

        var matches = exact is not null ? all : ChatCommandCatalog.StartingWith(all, name);
        if (matches.Count == 1 && matches[0].Name != HelpName)
        {
            player.PrintToChat($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} → !{matches[0].Name} {rest}");
            player.ExecuteClientCommandFromServer($"css_{matches[0].Name} {rest}".TrimEnd());
            return done;
        }

        if (matches.Count == 0)
        {
            player.PrintToChat($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Kein Befehl beginnt mit „{name}“. Tippe ! für alle Befehle.");
            return done;
        }

        string title = name.Length == 0 || exact is not null ? "Alle Befehle:" : $"Befehle mit „{name}“:";
        player.PrintToChat($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} {title}");
        foreach (var command in matches)
        {
            player.PrintToChat($"  {ChatColors.Gold}{ChatCommandCatalog.HelpLine(command)}");
        }
        return done;
    }
}
