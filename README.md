# Kyle Reese

> *"Come with me if you want to live."* — a one-click Windows tray panic button that
> force-stops runaway process trees spawned by Claude Code.

[![Release](https://github.com/whoiskevinrich/kyle-reese/actions/workflows/release.yml/badge.svg)](https://github.com/whoiskevinrich/kyle-reese/actions/workflows/release.yml)

🤖 **This project was built 100% with [Claude Code](https://claude.com/claude-code)** —
scaffolding, application code, tests, CI/CD, icon, and this README. Not a single line was
written by hand.

## Why this exists

There is a current bug in Claude Code that can cause it to **spawn runaway processes** — orphaned
`claude`, `bash`, `git`, and `sh` processes that keep piling up and consuming resources. It hit me
locally: dozens of stray processes left running, with no quick way to clear them all at once.

**Kyle Reese** is the fix I needed: a system-tray utility I can click to terminate the whole mess
instantly. It automates what I was otherwise doing by hand in PowerShell:

```powershell
"claude", "bash", "git", "sh" | ForEach-Object {
    Get-Process $_ -ErrorAction SilentlyContinue | Stop-Process -Force
}
```

(The name is a nod to *The Terminator* — Kyle Reese is the one sent back to stop the runaway
machines.)

## What it does

Click the tray icon (or use its menu) and Kyle Reese will:

1. **Find** every running process matching the configurable kill list.
2. **Confirm** with you, showing a count of what's about to be terminated.
3. **Kill the whole process tree** of each match — not just the named process — so spawned
   children and grandchildren die too (`taskkill /PID <pid> /T /F`).
4. **Report** how many process trees were actually terminated.

### Design guarantees

- **Whole-tree termination.** Killing only named processes can orphan their children; Kyle Reese
  takes down the entire descendant tree.
- **No admin elevation.** Runs un-elevated (`asInvoker`) and only touches *your* processes —
  attempts to kill another user's process simply fail and are reported, never escalated.
- **Configurable, not hardcoded.** The kill list lives in editable JSON; extend it without
  recompiling.
- **Confirm before killing.** No silent force-kills.

## Install

Grab the latest **`KyleReese.exe`** from the
[Releases page](https://github.com/whoiskevinrich/kyle-reese/releases/latest).

It's a single-file, self-contained executable — **no .NET runtime required**. Double-click it and a
red icon appears in your system tray. (Right-click → **Exit** to quit.)

## Usage

- **Left-click (double-click) the tray icon** or choose **Stop runaway processes** to trigger a kill.
- **Edit kill list…** opens `killlist.json` so you can change which process names are targeted.
- **Exit** closes the app.

### Configuration

The kill list is stored in `killlist.json` next to the executable. If the file is missing, the
defaults are used:

```json
{
  "processNames": [ "claude", "bash", "git", "sh" ]
}
```

Names are matched without the `.exe` extension and case-insensitively. If the file is missing or
malformed, Kyle Reese safely falls back to the defaults.

## Build from source

Requires the **.NET 10 SDK** (pinned via `global.json`).

```powershell
dotnet build KyleReese.slnx -c Release          # build
dotnet test  KyleReese.slnx -c Release          # run tests
dotnet publish src/KyleReese/KyleReese.csproj -c Release -o publish   # single-file exe
```

### Project layout

| Path | What |
|------|------|
| `src/KyleReese.Core/` | `net10.0` library with the testable logic — `ProcessKiller`, `KillListConfig`, `IProcessProvider`/`SystemProcessProvider`. No WinForms dependency. |
| `src/KyleReese/` | `net10.0-windows` WinForms tray app (`Program.cs`, `TrayApplicationContext.cs`). |
| `tests/KyleReese.Core.Tests/` | xunit tests against the core logic using a fake process provider. |
| `tools/make-icon.ps1` | Regenerates `app.ico` from `tools/icon-source.jpg`. |
| `.github/workflows/` | CI (build + test on PRs) and Release (semver tag + GitHub Release on push to `main`). |

### Releases & versioning

Pushing to `main` triggers the Release workflow, which derives a
[semantic version](https://semver.org/) from **conventional commits**
(`feat:` → minor, `!:`/`BREAKING CHANGE` → major, anything else → patch), tags it, and publishes
`KyleReese.exe` to a GitHub Release.

## Disclaimer

This is a personal utility and is **not affiliated with or endorsed by Anthropic**. It force-kills
processes by name — review the kill list before using it so you don't terminate something you care
about (e.g. an unrelated `git` operation).
