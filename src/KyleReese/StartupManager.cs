using Microsoft.Win32;

namespace KyleReese;

/// <summary>
/// Toggles whether the app launches at user logon via the per-user
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> registry key. This needs no admin
/// elevation and only affects the current user, matching the app's no-elevation policy.
/// </summary>
internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KyleReese";

    /// <summary>The path to the running executable, quoted for the Run key.</summary>
    private static string ExecutableCommand => $"\"{Environment.ProcessPath}\"";

    /// <summary>
    /// True when the Run key points at the currently running executable. A stale entry (e.g. the
    /// exe was moved) reads as disabled so the next enable rewrites the correct path.
    /// </summary>
    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) as string == ExecutableCommand;
    }

    /// <summary>Adds (or refreshes) the Run-key entry so the app starts with Windows.</summary>
    public static void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ValueName, ExecutableCommand);
    }

    /// <summary>Removes the Run-key entry, if present.</summary>
    public static void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>
    /// If startup is already registered but the entry points at a different path (e.g. the exe was
    /// moved or updated), rewrites it to the current executable. Does nothing when no entry exists,
    /// so it never enables startup on its own.
    /// </summary>
    public static void RefreshIfRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key?.GetValue(ValueName) is string existing && existing != ExecutableCommand)
        {
            key.SetValue(ValueName, ExecutableCommand);
        }
    }
}
