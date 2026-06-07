namespace KyleReese.Core;

/// <summary>
/// Finds and force-kills runaway process trees. Stateless apart from the injected
/// <see cref="IProcessProvider"/>, which keeps it fully unit-testable.
/// </summary>
public sealed class ProcessKiller
{
    private readonly IProcessProvider _provider;

    public ProcessKiller(IProcessProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <summary>Returns the currently-running processes matching <paramref name="names"/>.</summary>
    public IReadOnlyList<TargetProcess> Find(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        return _provider.FindByNames(names);
    }

    /// <summary>
    /// Force-kills the whole tree of each target. Callers typically <see cref="Find"/> first,
    /// confirm with the user, then pass the same list here.
    /// </summary>
    public KillResult Kill(IEnumerable<TargetProcess> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        var killed = new List<TargetProcess>();
        var failed = new List<TargetProcess>();

        foreach (var target in targets)
        {
            if (_provider.KillTree(target.Pid))
            {
                killed.Add(target);
            }
            else
            {
                failed.Add(target);
            }
        }

        return new KillResult(killed, failed);
    }
}
