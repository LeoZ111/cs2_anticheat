namespace AntiCheat.Core.TestMode;

/// <summary>A pro team whose players exist as bot profiles on the test server (from CS2-Bot-Improver's botprofile.vpk).</summary>
public sealed record ProTeam(string Name, IReadOnlyList<string> Players);

/// <summary>Opponents for the "legend" bot level.</summary>
public static class ProTeams
{
    // CS2 team numbers.
    public const int Terrorists = 2;
    public const int CounterTerrorists = 3;

    public static readonly IReadOnlyList<ProTeam> All = new[]
    {
        new ProTeam("Team Vitality", new[] { "apEX", "ZywOo", "ropz", "mezii", "flameZ" }),
        new ProTeam("Spirit", new[] { "sh1ro", "magixx", "tN1R", "zont1x", "donk" }),
        new ProTeam("Falcons", new[] { "NiKo", "TeSeS", "m0NESY", "karrigan", "kyousuke" }),
        new ProTeam("FURIA Esports", new[] { "yuurih", "FalleN", "KSCERATO", "YEKINDAR", "molodoy" }),
        new ProTeam("Natus Vincere", new[] { "Aleksib", "iM", "b1t", "w0nderful", "makazze" }),
    };

    public static ProTeam Pick(Random random) => All[random.Next(All.Count)];

    /// <summary>
    /// The side the pro team plays on: the opposite of where most humans are.
    /// Without humans on a team, the pros play CT.
    /// </summary>
    public static int EnemySideOf(IEnumerable<int> humanTeams)
    {
        var teams = humanTeams.ToList();
        int onT = teams.Count(t => t == Terrorists);
        int onCt = teams.Count(t => t == CounterTerrorists);
        if (onT == 0 && onCt == 0)
        {
            return CounterTerrorists;
        }
        return onT >= onCt ? CounterTerrorists : Terrorists;
    }
}
