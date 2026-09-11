namespace CloudNeko;

/// <summary>
/// Стан, який відображає нео-тян: що зараз "робить" Claude Code.
/// </summary>
public enum NekoState
{
    /// <summary>Чекає на новий запит від користувача (простій).</summary>
    Waiting,

    /// <summary>Виконує роботу (інструменти, генерація відповіді).</summary>
    Working,

    /// <summary>Чекає на відповідь користувача (опитування / дозвіл / AskUserQuestion).</summary>
    Polling,

    /// <summary>Щойно завершила таску — коротке святкування, потім повертається у Waiting.</summary>
    Done,
}

public static class NekoStateParser
{
    public static bool TryParse(string? raw, out NekoState state)
    {
        switch (raw?.Trim().ToLowerInvariant())
        {
            case "waiting":
            case "idle":
            case "wait":
                state = NekoState.Waiting;
                return true;
            case "working":
            case "work":
            case "busy":
                state = NekoState.Working;
                return true;
            case "polling":
            case "poll":
            case "question":
            case "ask":
                state = NekoState.Polling;
                return true;
            case "done":
            case "finished":
            case "complete":
                state = NekoState.Done;
                return true;
            default:
                state = NekoState.Waiting;
                return false;
        }
    }
}
