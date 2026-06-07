using KyleReese.Core;

namespace KyleReese.Core.Tests;

/// <summary>In-memory <see cref="IProcessProvider"/> for testing kill logic without real processes.</summary>
internal sealed class FakeProcessProvider : IProcessProvider
{
    private readonly IReadOnlyList<TargetProcess> _found;
    private readonly Func<int, bool> _killBehavior;

    public FakeProcessProvider(IReadOnlyList<TargetProcess> found, Func<int, bool>? killBehavior = null)
    {
        _found = found;
        _killBehavior = killBehavior ?? (_ => true);
    }

    /// <summary>Names passed to each <see cref="FindByNames"/> call, in order.</summary>
    public List<string[]> FindCalls { get; } = new();

    /// <summary>PIDs passed to each <see cref="KillTree"/> call, in order.</summary>
    public List<int> KilledPids { get; } = new();

    public IReadOnlyList<TargetProcess> FindByNames(IEnumerable<string> names)
    {
        FindCalls.Add(names.ToArray());
        return _found;
    }

    public bool KillTree(int pid)
    {
        KilledPids.Add(pid);
        return _killBehavior(pid);
    }
}
