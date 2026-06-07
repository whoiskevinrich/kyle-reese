# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

**Kyle Reese** is a **Windows 11 system-tray "panic button"** — a single click force-stops
runaway process trees spawned by Claude Code. (The repo/project codename is `kyle-reese`;
the user-facing app name is "Kyle Reese".) The reference behavior it automates:

```powershell
"claude", "bash", "git", "sh" | ForEach-Object {
    Get-Process $_ -ErrorAction SilentlyContinue | Stop-Process -Force
}
```

## Layout

- `src/KyleReese.Core/` — `net10.0` class library with the testable logic: `ProcessKiller`,
  `KillListConfig`, `IProcessProvider` (+ `SystemProcessProvider`, the Windows `taskkill`
  implementation). No WinForms dependency, so it unit-tests cross-platform.
- `src/KyleReese/` — `net10.0-windows` WinForms tray app (`Program.cs`,
  `TrayApplicationContext.cs`). UI only; all kill/config logic lives in Core.
- `tests/` — xunit tests against Core using a mock `IProcessProvider`.
- Solution file is `KyleReese.slnx` (the .NET 10 XML solution format).

## Build / test / publish

The SDK is pinned to .NET 10 via `global.json`. Commands:

```powershell
dotnet build KyleReese.slnx -c Release
dotnet test  KyleReese.slnx -c Release
# Single-file, self-contained win-x64 exe (RID + self-contained + single-file are set in the csproj):
dotnet publish src/KyleReese/KyleReese.csproj -c Release -o publish
```

## Code style

- Analyzers are enforced repo-wide via `Directory.Build.props`:
  `latest-recommended` analysis, `EnforceCodeStyleInBuild`, and **`TreatWarningsAsErrors`** —
  the build fails on any warning, so keep it clean.
- `.editorconfig` rules: file-scoped namespaces, `using` outside the namespace, braces always,
  private fields as `_camelCase`, 4-space indent (2 for json/yaml/csproj).
- Don't make `IProcessProvider` member implementations static and keep Core free of WinForms
  so the test project stays cross-platform.

## Behavioral requirements (must hold)

- **Kill the whole process tree, not just named processes.** `SystemProcessProvider.KillTree`
  uses `taskkill /PID <pid> /T /F` so spawned children/grandchildren die too. Don't regress to
  killing processes by name alone.
- **The kill list is configuration, not hardcoded.** Names live in `killlist.json` beside the
  exe (`KillListConfig`), defaulting to `claude`/`bash`/`git`/`sh`. Loading must never throw —
  fall back to defaults on missing/malformed files.
- **No admin elevation.** `app.manifest` requests `asInvoker`; only the current user's
  processes get killed. Don't add an elevation manifest or require Administrator.
- **Confirm before killing, and report results.** The tray flow finds matches, confirms with a
  count, then kills and reports how many trees were terminated.
