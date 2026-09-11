using System.IO;
using System.Windows.Threading;

namespace CloudNeko;

/// <summary>
/// Слідкує за станами кількох паралельних сесій (%LOCALAPPDATA%\CloudNeko\sessions\&lt;id&gt;.txt,
/// один файл на сесію) і агрегує їх в один ефективний стан для відображення:
/// polling переважає working переважає waiting. "done" — разова подія (як і "pet"),
/// а не стан, що зберігається — після неї сесія вважається знову waiting.
/// Сесії, чий файл не оновлювався довше <see cref="StaleAfter"/>, ігноруються
/// в агрегації (вважаються мертвими/закритими без коректного SessionEnd).
/// </summary>
public sealed class StateFileWatcher : IDisposable
{
    private static readonly TimeSpan StaleAfter = TimeSpan.FromHours(4);
    private static string SessionsDirectory => SessionPaths.SessionsDirectory;

    /// <summary>Агрегований стан (waiting/working/polling) для відображення.</summary>
    public event Action<NekoState>? StateChanged;

    /// <summary>Разова реакція "погладили" — не персистентний стан.</summary>
    public event Action? PetRequested;

    /// <summary>Разова подія "якась сесія щойно завершила таску" — коротке святкування.</summary>
    public event Action? DoneCelebrationRequested;

    private readonly System.Windows.Threading.Dispatcher _uiDispatcher = Dispatcher.CurrentDispatcher;
    private readonly FileSystemWatcher _watcher;
    private readonly Dictionary<string, NekoState> _sessionStates = new();
    private readonly Dictionary<string, string> _lastRawText = new();

    public StateFileWatcher()
    {
        Directory.CreateDirectory(SessionsDirectory);
        MigrateLegacyStateFileIfAny();

        foreach (var path in Directory.EnumerateFiles(SessionsDirectory, "*.txt"))
        {
            ProcessFile(path, raise: false);
        }

        _watcher = new FileSystemWatcher(SessionsDirectory, "*.txt")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
        };
        _watcher.Changed += (_, e) => Dispatch(() => ProcessFile(e.FullPath, raise: true));
        _watcher.Created += (_, e) => Dispatch(() => ProcessFile(e.FullPath, raise: true));
        _watcher.Renamed += (_, e) => Dispatch(() => ProcessFile(e.FullPath, raise: true));
        _watcher.Deleted += (_, e) => Dispatch(() => RemoveSession(IdFromPath(e.FullPath), raise: true));
        _watcher.EnableRaisingEvents = true;
    }

    public NekoState ReadCurrent() => Aggregate();

    private void Dispatch(Action action) => _uiDispatcher.BeginInvoke(action);

    private static string IdFromPath(string path) => Path.GetFileNameWithoutExtension(path);

    private void MigrateLegacyStateFileIfAny()
    {
        if (!File.Exists(SessionPaths.LegacyStateFilePath))
        {
            return;
        }

        try
        {
            string text = File.ReadAllText(SessionPaths.LegacyStateFilePath);
            if (NekoStateParser.TryParse(text, out var state) && state != NekoState.Done)
            {
                File.WriteAllText(SessionPaths.SessionFilePath(SessionPaths.ManualSessionId), text.Trim().ToLowerInvariant());
            }

            File.Delete(SessionPaths.LegacyStateFilePath);
        }
        catch (IOException)
        {
            // Не критично — просто лишиться для наступного разу.
        }
    }

    private void ProcessFile(string path, bool raise)
    {
        string id = IdFromPath(path);
        string text;

        try
        {
            text = ReadAllTextWithRetry(path);
        }
        catch (IOException)
        {
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        if (_lastRawText.TryGetValue(id, out var previous) && previous == text)
        {
            return; // той самий запис (дублюючі FileSystemWatcher-події) — нічого нового
        }
        _lastRawText[id] = text;

        string trimmed = text.Trim().ToLowerInvariant();
        if (trimmed is "pet" or "headpat" or "pat")
        {
            if (raise)
            {
                PetRequested?.Invoke();
            }
            return;
        }

        if (!NekoStateParser.TryParse(text, out var state))
        {
            return;
        }

        if (state == NekoState.Done)
        {
            _sessionStates[id] = NekoState.Waiting;
            if (raise)
            {
                DoneCelebrationRequested?.Invoke();
            }
        }
        else
        {
            _sessionStates[id] = state;
        }

        RecomputeAndRaise(raise);
    }

    private void RemoveSession(string id, bool raise)
    {
        _sessionStates.Remove(id);
        _lastRawText.Remove(id);
        RecomputeAndRaise(raise);
    }

    private void RecomputeAndRaise(bool raise)
    {
        var aggregate = Aggregate();
        if (raise)
        {
            StateChanged?.Invoke(aggregate);
        }
    }

    private NekoState Aggregate()
    {
        var now = DateTime.UtcNow;
        NekoState best = NekoState.Waiting;
        int bestPriority = 0;
        List<string>? stale = null;

        foreach (var (id, state) in _sessionStates)
        {
            var mtime = File.GetLastWriteTimeUtc(SessionPaths.SessionFilePath(id));
            if (now - mtime > StaleAfter)
            {
                (stale ??= new List<string>()).Add(id);
                continue;
            }

            int priority = state switch
            {
                NekoState.Polling => 3,
                NekoState.Working => 2,
                _ => 1,
            };

            if (priority > bestPriority)
            {
                bestPriority = priority;
                best = state;
            }
        }

        if (stale != null)
        {
            foreach (var id in stale)
            {
                _sessionStates.Remove(id);
                _lastRawText.Remove(id);
            }
        }

        return best;
    }

    private static string ReadAllTextWithRetry(string path)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(20);
            }
        }

        return File.ReadAllText(path);
    }

    public void Dispose()
    {
        _watcher.Dispose();
    }
}
