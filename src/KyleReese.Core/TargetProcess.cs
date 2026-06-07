namespace KyleReese.Core;

/// <summary>A running process matched by name that is a candidate for termination.</summary>
/// <param name="Pid">The OS process id.</param>
/// <param name="Name">The process name (without the <c>.exe</c> extension).</param>
public readonly record struct TargetProcess(int Pid, string Name);
