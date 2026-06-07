namespace KyleReese.Core;

/// <summary>
/// Abstraction over the OS process table so kill logic can be unit-tested without
/// spawning real processes.
/// </summary>
public interface IProcessProvider
{
    /// <summary>Returns running processes whose name matches any of <paramref name="names"/>.</summary>
    IReadOnlyList<TargetProcess> FindByNames(IEnumerable<string> names);

    /// <summary>
    /// Force-terminates the process identified by <paramref name="pid"/> together with its
    /// entire descendant tree. Returns <c>true</c> on success.
    /// </summary>
    bool KillTree(int pid);
}
