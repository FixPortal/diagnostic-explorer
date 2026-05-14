# 3.1.38 — rationale

This document covers the `3.1.38` cut. See `RECUT-3.1.37.md` (or
git history) for the `3.1.37` story.

## TL;DR

`3.1.38` is a single-purpose follow-up to `3.1.37` that fixes how
the registering process names itself when reporting to the
diagnostic hub. Nothing else changed: the API surface, lifecycle
fixes, and vulnerability bumps from `3.1.37` are all unchanged.

## The bug

`DiagnosticHostingService.StartHosting` builds the `Registration`
payload using:

```csharp
ProcessName = Process.GetCurrentProcess().ProcessName.Replace(".vshost", "")
```

`Process.GetCurrentProcess().ProcessName` returns the OS-level
process name. For a `dotnet MyApp.dll` launch (typical for Linux
containers, Kestrel hosts, `dotnet run`, etc.), that's `dotnet` —
not `MyApp`. So every `dotnet`-launched diagnostic-emitting client
shows up in the dashboard as a generic `dotnet`, indistinguishable
from any other.

For a Windows native exe (typical when running the published
self-contained app or a Debug build with `UseAppHost=true`), the
process name *is* the app name, so the old behaviour happened to
work. The bug was latent until anyone ran the app via the dotnet
CLI host — at which point all such instances collided under the
same display name.

## The fix

Prefer the entry assembly's name when one is available. That value
is stable across both launch modes:

```csharp
private static string ResolveProcessName()
{
    string entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
    if (!string.IsNullOrEmpty(entryAssemblyName))
        return entryAssemblyName;

    return Process.GetCurrentProcess().ProcessName.Replace(".vshost", "");
}
```

`Assembly.GetEntryAssembly()` returns `null` only when the runtime
was started from unmanaged code with no managed entry assembly —
mostly hosting scenarios that don't apply here, but worth the
fallback. The `.vshost` strip is preserved for the legacy
Visual Studio debug host case (largely defunct but harmless).

For `DiagnosticExplorer.Service` running as `dotnet
DiagnosticExplorer.Service.dll`:

- Before (`3.1.37`): `ProcessName = "dotnet"`
- After (`3.1.38`): `ProcessName = "DiagnosticExplorer.Service"`

## Scope

One source file (`DiagnosticExplorer.Hosting/DiagnosticHostingService.cs`),
plus the version bumps in both csprojs and the regenerated nupkgs
under `nupkg/`. Consumers upgrading from `3.1.37` need no source
changes; the registered display name simply becomes more useful on
next restart.

## Consumer migration

- **From `3.1.37`**: bump the pinned version, restore, rebuild,
  restart. No source change required. On restart, each
  diagnostic-emitting client will report with its entry-assembly
  name instead of `dotnet` (where that previously happened).
- **From `3.1.35` / older**: see `RECUT-3.1.37.md` for the
  intermediate API restoration and vulnerability-bump story; this
  cut adds the process-name fix on top.
