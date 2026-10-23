using AntiCheat.Core.TestMode;

namespace AntiCheat.Core.Tests.TestMode;

public class ChatCommandCatalogTests
{
    private static readonly ChatCommandInfo[] Commands =
    {
        new("help", "", "Alle Befehle"),
        new("bots", "[rush|hold]", "Bots steuern"),
        new("bot_skill", "[noob|pro]", "Bot-Stärke"),
        new("cash", "", "Geld"),
    };

    [Fact]
    public void EmptyBeginningListsAllCommandsAlphabetically()
    {
        var names = ChatCommandCatalog.StartingWith(Commands, "").Select(c => c.Name);
        Assert.Equal(new[] { "bot_skill", "bots", "cash", "help" }, names);
    }

    [Fact]
    public void BeginningFiltersAndSorts()
    {
        var names = ChatCommandCatalog.StartingWith(Commands, "b").Select(c => c.Name);
        Assert.Equal(new[] { "bot_skill", "bots" }, names);
    }

    [Fact]
    public void BeginningIgnoresUpperAndLowerCase()
    {
        Assert.Single(ChatCommandCatalog.StartingWith(Commands, "CA"));
    }

    [Fact]
    public void UnknownBeginningGivesNothing()
    {
        Assert.Empty(ChatCommandCatalog.StartingWith(Commands, "x"));
    }

    [Fact]
    public void ExactNameIsFound()
    {
        Assert.Equal("bots", ChatCommandCatalog.Exact(Commands, "BOTS")?.Name);
        Assert.Null(ChatCommandCatalog.Exact(Commands, "bot"));
    }

    [Theory]
    [InlineData("!bot_s pro", "bot_s", "pro")]
    [InlineData("/bot_skill  legend ", "bot_skill", "legend")]
    [InlineData("!", "", "")]
    [InlineData("\"!c\"", "c", "")]
    public void MessagesAreSplitIntoCommandAndRest(string message, string command, string rest)
    {
        Assert.Equal((command, rest), ChatCommandCatalog.Split(message));
    }

    [Theory]
    [InlineData("hallo")]
    [InlineData("")]
    public void NormalChatIsNotACommand(string message)
    {
        Assert.Null(ChatCommandCatalog.Split(message));
    }

    [Fact]
    public void HelpLineShowsNameArgumentsAndDescription()
    {
        Assert.Equal("!bot_skill [noob|pro] – Bot-Stärke", ChatCommandCatalog.HelpLine(Commands[2]));
        Assert.Equal("!cash – Geld", ChatCommandCatalog.HelpLine(Commands[3]));
    }
}
