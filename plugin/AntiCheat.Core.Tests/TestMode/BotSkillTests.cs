using AntiCheat.Core.TestMode;

namespace AntiCheat.Core.Tests.TestMode;

public class BotSkillTests
{
    [Theory]
    [InlineData("noob", BotSkillLevel.Noob)]
    [InlineData("mid", BotSkillLevel.Mid)]
    [InlineData("hard", BotSkillLevel.Hard)]
    [InlineData("pro", BotSkillLevel.Pro)]
    [InlineData("legend", BotSkillLevel.Legend)]
    [InlineData("  PRO ", BotSkillLevel.Pro)]
    [InlineData("expert", BotSkillLevel.Pro)]
    public void KnownNamesAreRead(string text, BotSkillLevel expected)
    {
        Assert.Equal(expected, BotSkill.Parse(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("godmode")]
    [InlineData(null)]
    public void UnknownNamesGiveNull(string? text)
    {
        Assert.Null(BotSkill.Parse(text));
    }

    [Fact]
    public void LevelsGetStrongerStepByStep()
    {
        var difficulties = Enum.GetValues<BotSkillLevel>().Select(l => BotSkill.SettingsFor(l).BotDifficulty).ToList();
        Assert.Equal(new[] { 0, 1, 2, 3, 3 }, difficulties);
    }

    [Fact]
    public void OnlyLegendUsesAProTeam()
    {
        Assert.True(BotSkill.SettingsFor(BotSkillLevel.Legend).UsesProTeam);
        Assert.False(BotSkill.SettingsFor(BotSkillLevel.Pro).UsesProTeam);
    }

    [Fact]
    public void HelpTextListsAllLevels()
    {
        Assert.Equal("noob, mid, hard, pro, legend", BotSkill.AllNames);
    }
}
