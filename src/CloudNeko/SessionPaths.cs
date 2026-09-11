using System.IO;
using System.Linq;

namespace CloudNeko;

/// <summary>
/// Шляхи до файлів стану сесій. Без залежностей від WPF — використовується і
/// GUI-застосунком (CloudNeko.exe), і легким CLI (CloudNekoCli.exe).
/// </summary>
public static class SessionPaths
{
    public const string ManualSessionId = "_manual";

    public static string BaseDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CloudNeko");

    public static string SessionsDirectory { get; } = Path.Combine(BaseDirectory, "sessions");

    public static string LegacyStateFilePath { get; } = Path.Combine(BaseDirectory, "state.txt");

    public static string SanitizeSessionId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ManualSessionId;
        }

        var chars = raw.Trim().Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray();
        string sanitized = new string(chars);
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }

    public static string SessionFilePath(string sessionId) =>
        Path.Combine(SessionsDirectory, SanitizeSessionId(sessionId) + ".txt");
}
