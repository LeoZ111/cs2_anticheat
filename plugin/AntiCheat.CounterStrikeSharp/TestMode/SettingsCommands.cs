using AntiCheat.Core.TestMode;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace AntiCheat.CounterStrikeSharp.TestMode;

/// <summary>
/// Chat commands to change the test server: !map, !money, !warmup, !roundtime, !nades, !bot_weapon.
/// The parsing lives in <see cref="ServerSettings"/> (core, with tests); this class only runs the commands.
/// </summary>
public static class SettingsCommands
{
    public static readonly IReadOnlyList<ChatCommandInfo> Infos = new[]
    {
        new ChatCommandInfo("map", "<name>", "Karte wechseln"),
        new ChatCommandInfo("money", "[max|normal|<betrag>]", "Geld einstellen"),
        new ChatCommandInfo("warmup", "[on|off]", "Aufwärmphase starten oder beenden"),
        new ChatCommandInfo("roundtime", "<minuten>", "Rundenzeit ab der nächsten Runde"),
        new ChatCommandInfo("nades", "[" + string.Join("|", ServerSettings.NadeModes) + "]", "Wie viele Granaten Bots werfen"),
        new ChatCommandInfo("bot_weapon", "<waffe>|normal", "Waffe aller Bots ab der nächsten Runde"),
    };

    public static void Register(BasePlugin plugin)
    {
        Add(plugin, "map", OnMap);
        Add(plugin, "money", OnMoney);
        Add(plugin, "warmup", (p, arg, reply) => RunSimple(p, arg, reply, ServerSettings.Warmup(arg), "on, off", $"Aufwärmphase: {arg}"));
        Add(plugin, "roundtime", (p, arg, reply) => RunSimple(p, arg, reply, ServerSettings.RoundTime(arg), "z. B. 2 oder 1,5 (0,5 bis 60)", $"Rundenzeit: {arg} Minuten (ab der nächsten Runde)"));
        Add(plugin, "nades", (p, arg, reply) => RunSimple(p, arg, reply, ServerSettings.Nades(arg), string.Join(", ", ServerSettings.NadeModes), $"Bot-Granaten: {arg}"));
        Add(plugin, "bot_weapon", OnBotWeapon);
    }

    private static void OnBotWeapon(BasePlugin plugin, string argument, Action<string> reply)
    {
        string? loadout = ServerSettings.BotLoadout(argument);
        ConVar? convar = ConVar.Find("bot_loadout");
        if (loadout is null || convar is null)
        {
            reply($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Möglich: normal, {string.Join(", ", ServerSettings.BotWeaponNames)}");
            return;
        }
        // bot_loadout is cheat protected for the console, but the server itself may set it.
        convar.Flags &= ~ConVarFlags.FCVAR_CHEAT;
        convar.StringValue = loadout;
        Announce(loadout.Length == 0 ? "Bots kaufen wieder normal" : $"Bot-Waffe: {argument} (ab der nächsten Runde)");
    }

    private delegate void Handler(BasePlugin plugin, string argument, Action<string> reply);

    private static void Add(BasePlugin plugin, string name, Handler handler)
    {
        plugin.AddCommand("css_" + name, Infos.First(i => i.Name == name).Description, (player, command) =>
        {
            string argument = command.ArgCount > 1 ? command.ArgString.Trim() : "";
            plugin.Logger.LogInformation("{Command} '{Argument}' from {Player}", name, argument, player?.PlayerName ?? "console");
            handler(plugin, argument, text => command.ReplyToCommand(text));
        });
    }

    private static void RunSimple(BasePlugin plugin, string argument, Action<string> reply, IReadOnlyList<string>? commands, string options, string announcement)
    {
        if (commands is null)
        {
            reply($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Möglich: {options}");
            return;
        }
        foreach (string command in commands)
        {
            Server.ExecuteCommand(command);
        }
        Announce(announcement);
    }

    private static void OnMap(BasePlugin plugin, string argument, Action<string> reply)
    {
        string? map = ServerSettings.ResolveMap(argument);
        if (map is null)
        {
            reply($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Karten: {string.Join(", ", ServerSettings.Maps.Select(m => m[3..]))}");
            return;
        }
        // An empty server hibernates and runs no timers, so then we switch right away.
        if (!Utilities.GetPlayers().Any(p => p.IsValid && !p.IsBot && !p.IsHLTV))
        {
            Server.ExecuteCommand($"changelevel {map}");
            return;
        }
        Announce($"Kartenwechsel zu {map} in 3 Sekunden");
        plugin.AddTimer(3f, () => Server.ExecuteCommand($"changelevel {map}"));
    }

    private static void OnMoney(BasePlugin plugin, string argument, Action<string> reply)
    {
        MoneySettings? money = ServerSettings.ParseMoney(argument);
        if (money is null)
        {
            reply($" {ChatColors.Red}[AntiCheat]{ChatColors.Default} Möglich: max, normal oder ein Betrag (0 bis 65535)");
            return;
        }
        foreach (string command in money.Commands)
        {
            Server.ExecuteCommand(command);
        }

        // mp_startmoney only counts from the next round, so we also set everyone's money right now.
        foreach (var player in Utilities.GetPlayers().Where(p => p.IsValid && p.InGameMoneyServices is not null))
        {
            player.InGameMoneyServices!.Account = money.StartMoney;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInGameMoneyServices");
        }
        string anywhere = money.BuyAnywhere ? ", kaufen überall" : "";
        Announce($"Geld: ${money.StartMoney}{anywhere}");
    }

    private static void Announce(string text)
    {
        Server.PrintToChatAll($" {ChatColors.Green}[AntiCheat]{ChatColors.Default} {text}");
    }
}
