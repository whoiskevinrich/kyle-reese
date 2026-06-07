using System.Diagnostics;
using KyleReese.Core;

namespace KyleReese;

/// <summary>
/// Hosts the system-tray icon. A click (or the context menu) finds runaway processes from the
/// configurable kill list, asks for confirmation, force-kills the whole tree of each, and reports
/// how many were terminated.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const string AppName = "Kyle Reese";

    private readonly NotifyIcon _trayIcon;
    private readonly ProcessKiller _killer = new(new SystemProcessProvider());

    public TrayApplicationContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("&Stop runaway processes", null, (_, _) => StopProcesses());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("&Edit kill list…", null, (_, _) => EditKillList());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("E&xit", null, (_, _) => ExitApp());

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Error,
            Text = $"{AppName} — click to stop runaway processes",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _trayIcon.DoubleClick += (_, _) => StopProcesses();
    }

    private void StopProcesses()
    {
        var config = KillListConfig.Load(KillListConfig.DefaultConfigPath);
        var targets = _killer.Find(config.ProcessNames);

        if (targets.Count == 0)
        {
            _trayIcon.ShowBalloonTip(3000, AppName, "No matching processes are running.", ToolTipIcon.Info);
            return;
        }

        var summary = string.Join(
            Environment.NewLine,
            targets
                .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => $"  • {g.Key} ({g.Count()})"));

        var confirm = MessageBox.Show(
            $"Force-kill {targets.Count} process tree(s)?{Environment.NewLine}{Environment.NewLine}{summary}",
            AppName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        var result = _killer.Kill(targets);

        var message = $"Killed {result.KilledCount} process tree(s).";
        if (result.FailedCount > 0)
        {
            message += $"{Environment.NewLine}{result.FailedCount} could not be killed " +
                       "(another user's process, or already exited).";
        }

        _trayIcon.ShowBalloonTip(
            4000,
            AppName,
            message,
            result.FailedCount > 0 ? ToolTipIcon.Warning : ToolTipIcon.Info);
    }

    private static void EditKillList()
    {
        var path = KillListConfig.DefaultConfigPath;
        try
        {
            if (!File.Exists(path))
            {
                // Materialize the defaults so the user has something to edit.
                KillListConfig.Load(path).Save(path);
            }

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"Could not open the kill list:{Environment.NewLine}{ex.Message}",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ExitApp()
    {
        _trayIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
