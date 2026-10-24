namespace AntiCheat.Core.TestMode;

/// <summary>One chat command, as shown in the in-game help.</summary>
/// <param name="Name">Name without "!", e.g. "bot_skill".</param>
/// <param name="Arguments">What can follow the name, e.g. "[noob|mid|hard|pro|legend]". Empty if nothing.</param>
/// <param name="Description">Short German explanation for the help list.</param>
public sealed record ChatCommandInfo(string Name, string Arguments, string Description);

/// <summary>
/// Finds chat commands by their beginning. CS2 has no tab completion in chat,
/// so "!b" + Enter lists every command starting with "b", and a beginning that fits only one command runs it.
/// </summary>
public static class ChatCommandCatalog
{
    /// <summary>All commands whose name starts with <paramref name="prefix"/>, sorted alphabetically.</summary>
    public static IReadOnlyList<ChatCommandInfo> StartingWith(IEnumerable<ChatCommandInfo> commands, string prefix)
    {
        return commands
            .Where(c => c.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            // Lowercase + ordinal puts "_" before letters, like a file list in cmd: bot_skill before bots.
            .OrderBy(c => c.Name.ToLowerInvariant(), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>The command with exactly this name, or null.</summary>
    public static ChatCommandInfo? Exact(IEnumerable<ChatCommandInfo> commands, string name)
    {
        return commands.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Splits a chat message like "!bot_s pro" into the command part ("bot_s") and the rest ("pro").
    /// Returns null if the message does not start with ! or /.
    /// </summary>
    public static (string Command, string Arguments)? Split(string message)
    {
        string text = message.Trim().Trim('"').Trim();
        if (text.Length == 0 || (text[0] != '!' && text[0] != '/'))
        {
            return null;
        }
        string body = text[1..].TrimStart();
        int space = body.IndexOf(' ');
        return space < 0 ? (body, "") : (body[..space], body[(space + 1)..].Trim());
    }

    /// <summary>
    /// "!cheat esp bind:F5" -> ("cheat esp", "F5"). Returns null if the message has no valid bind:KEY part.
    /// Keys: letters, digits and names like F5, mouse4, kp_end.
    /// </summary>
    public static (string Command, string Key)? ExtractBind(string message)
    {
        var parts = Split(message);
        if (parts is null) return null;
        string full = (parts.Value.Command + " " + parts.Value.Arguments).Trim();
        var words = full.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        int index = words.FindIndex(w => w.StartsWith("bind:", StringComparison.OrdinalIgnoreCase));
        if (index < 0) return null;
        string key = words[index]["bind:".Length..];
        if (key.Length == 0 || !key.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) return null;
        words.RemoveAt(index);
        if (words.Count == 0) return null;
        return (string.Join(' ', words), key.ToLowerInvariant());
    }

    /// <summary>The console line that makes a key run a chat command silently, e.g. bind "f5" "say /cheat esp".</summary>
    public static string BindLine(string key, string command) => $"bind \"{key}\" \"say /{command}\"";

    /// <summary>One help line, e.g. "!bot_skill [noob|...] – Bot-Stärke einstellen".</summary>
    public static string HelpLine(ChatCommandInfo command)
    {
        string arguments = command.Arguments.Length > 0 ? " " + command.Arguments : "";
        return $"!{command.Name}{arguments} – {command.Description}";
    }
}
