using System.Diagnostics;

namespace KyleReese.Core;

/// <summary>
/// Real <see cref="IProcessProvider"/> backed by the Windows process table. Kills trees via
/// <c>taskkill /T /F</c>, which terminates spawned children/grandchildren too. Runs un-elevated:
/// attempts to kill another user's process simply fail (reported, not thrown), which is the
/// intended "current user only" behavior.
/// </summary>
public sealed class SystemProcessProvider : IProcessProvider
{
    public IReadOnlyList<TargetProcess> FindByNames(IEnumerable<string> names)
    {
        var wanted = names
            .Select(Normalize)
            .Where(n => n.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (wanted.Count == 0)
        {
            return Array.Empty<TargetProcess>();
        }

        var self = Environment.ProcessId;
        var matches = new List<TargetProcess>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id != self && wanted.Contains(process.ProcessName))
                {
                    matches.Add(new TargetProcess(process.Id, process.ProcessName));
                }
            }
            catch (InvalidOperationException)
            {
                // Process exited between enumeration and access; skip it.
            }
            finally
            {
                process.Dispose();
            }
        }

        return matches;
    }

    public bool KillTree(int pid)
    {
        try
        {
            var startInfo = new ProcessStartInfo("taskkill", $"/PID {pid} /T /F")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var proc = Process.Start(startInfo);
            if (proc is null)
            {
                return false;
            }

            proc.WaitForExit(10_000);
            return proc.HasExited && proc.ExitCode == 0;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private static string Normalize(string name)
    {
        name = name.Trim();
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
    }
}
