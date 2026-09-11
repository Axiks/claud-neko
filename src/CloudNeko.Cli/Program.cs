using System.Text.Json;
using CloudNeko;

// Легкий CLI без WPF — щоб старт займав десятки мс, а не сотні (він викликається
// на кожен PreToolUse/UserPromptSubmit, тож затримка тут напряму б'є по хукам).
//
// Використання:
//   CloudNekoCli --set-state <waiting|working|polling|done|pet> [--session <id>]
//   CloudNekoCli --end-session [<id>]
//
// Якщо --session не задано явно, id сесії читається з JSON на stdin (стандартний
// формат Claude Code hooks: {"session_id": "...", ...}). Без stdin і без --session
// пише у спільну сесію "_manual".

int setStateIdx = Array.IndexOf(args, "--set-state");
if (setStateIdx >= 0 && setStateIdx + 1 < args.Length)
{
    string requested = args[setStateIdx + 1];
    string trimmed = requested.Trim().ToLowerInvariant();
    bool isPet = trimmed is "pet" or "headpat" or "pat";

    if (!isPet && !NekoStateParser.TryParse(requested, out _))
    {
        Console.Error.WriteLine($"Unknown state '{requested}'. Use: waiting | working | polling | done | pet");
        return 1;
    }

    string sessionId = ResolveSessionId(args);
    Directory.CreateDirectory(SessionPaths.SessionsDirectory);
    File.WriteAllText(SessionPaths.SessionFilePath(sessionId), trimmed);
    return 0;
}

int endSessionIdx = Array.IndexOf(args, "--end-session");
if (endSessionIdx >= 0)
{
    string sessionId = endSessionIdx + 1 < args.Length && !args[endSessionIdx + 1].StartsWith("--")
        ? args[endSessionIdx + 1]
        : ResolveSessionId(args);

    string path = SessionPaths.SessionFilePath(sessionId);
    if (File.Exists(path))
    {
        File.Delete(path);
    }

    return 0;
}

Console.Error.WriteLine("Usage: CloudNekoCli --set-state <waiting|working|polling|done|pet> [--session <id>] | --end-session [<id>]");
return 1;

static string ResolveSessionId(string[] args)
{
    int sessionIdx = Array.IndexOf(args, "--session");
    if (sessionIdx >= 0 && sessionIdx + 1 < args.Length)
    {
        return args[sessionIdx + 1];
    }

    return ReadSessionIdFromStdin();
}

static string ReadSessionIdFromStdin()
{
    try
    {
        if (!Console.IsInputRedirected)
        {
            return SessionPaths.ManualSessionId;
        }

        string raw = Console.In.ReadToEnd();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return SessionPaths.ManualSessionId;
        }

        using var doc = JsonDocument.Parse(raw);
        if (doc.RootElement.TryGetProperty("session_id", out var idProp))
        {
            string? id = idProp.GetString();
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }
        }
    }
    catch (JsonException)
    {
        // Немає/зіпсований JSON на stdin — падаємо на "_manual" нижче.
    }

    return SessionPaths.ManualSessionId;
}
