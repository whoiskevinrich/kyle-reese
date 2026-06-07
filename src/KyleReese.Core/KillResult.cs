namespace KyleReese.Core;

/// <summary>Outcome of a kill operation, split into successes and failures.</summary>
public sealed class KillResult
{
    public KillResult(IReadOnlyList<TargetProcess> killed, IReadOnlyList<TargetProcess> failed)
    {
        Killed = killed;
        Failed = failed;
    }

    /// <summary>Process trees that were successfully terminated.</summary>
    public IReadOnlyList<TargetProcess> Killed { get; }

    /// <summary>Process trees that could not be terminated (e.g. another user, already exited).</summary>
    public IReadOnlyList<TargetProcess> Failed { get; }

    public int KilledCount => Killed.Count;

    public int FailedCount => Failed.Count;
}
