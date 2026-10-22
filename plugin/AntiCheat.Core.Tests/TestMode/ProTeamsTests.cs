using AntiCheat.Core.TestMode;

namespace AntiCheat.Core.Tests.TestMode;

public class ProTeamsTests
{
    [Fact]
    public void EveryTeamHasFivePlayers()
    {
        Assert.All(ProTeams.All, team => Assert.Equal(5, team.Players.Count));
    }

    [Fact]
    public void NoPlayerIsInTwoTeams()
    {
        var players = ProTeams.All.SelectMany(t => t.Players).ToList();
        Assert.Equal(players.Count, players.Distinct().Count());
    }

    [Fact]
    public void PickAlwaysReturnsAKnownTeam()
    {
        var random = new Random(42);
        for (int i = 0; i < 20; i++)
        {
            Assert.Contains(ProTeams.Pick(random), ProTeams.All);
        }
    }

    [Theory]
    [InlineData(new[] { ProTeams.Terrorists }, ProTeams.CounterTerrorists)]
    [InlineData(new[] { ProTeams.CounterTerrorists }, ProTeams.Terrorists)]
    [InlineData(new[] { ProTeams.CounterTerrorists, ProTeams.CounterTerrorists, ProTeams.Terrorists }, ProTeams.Terrorists)]
    [InlineData(new int[0], ProTeams.CounterTerrorists)]
    [InlineData(new[] { 1 }, ProTeams.CounterTerrorists)] // spectators only
    public void ProsPlayAgainstTheHumans(int[] humanTeams, int expectedEnemySide)
    {
        Assert.Equal(expectedEnemySide, ProTeams.EnemySideOf(humanTeams));
    }
}
