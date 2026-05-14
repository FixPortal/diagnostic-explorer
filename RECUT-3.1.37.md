# 3.1.37 — rationale

This document explains why `3.1.37` exists. It supersedes both the
first `3.1.36` (`f6a2228`) and the recut `3.1.36` (`b328538`) on the
`anvil/fix-tracescope-lifecycle` branch. Anyone who has restored
either `3.1.36` from `LocalPackages` should upgrade to `3.1.37`.

## TL;DR

`3.1.37` rolls three concerns into one cut:

1. **The lifecycle / race-condition bug fixes** that motivated the
   original `3.1.36` version bump from `3.1.35` (subscription
   restart, stale callbacks, event-stream lifecycle, hub-response
   fire-and-forget, `TraceScope` async-stack).
2. **API surface preserved exactly as `3.1.35`** —
   `AddDiagnosticExplorer(IServiceCollection, IConfiguration,
   Action<HttpConnectionOptions> = null)`, plus the `HttpConnectionOptions`
   plumbing through `DiagnosticHostingService.Start` and
   `RegistrationHandler.Start`. The first `3.1.36` cut had silently
   removed these (which broke `EmsFramework.cs`'s `AccessTokenProvider`
   hook for Azure AD bearer-token auth on the diagnostic uplink).
3. **Known-vulnerability dependency bumps** —
   `log4net 3.0.1 → 3.3.1` (GHSA-4f7c-pmjv-c25w, moderate),
   `System.Text.Json 6.0.5 → 8.0.5` (GHSA-8g4q-xg66-9fp4, high),
   and `Microsoft.Bcl.AsyncInterfaces 6.0.0 → 8.0.0` (transitive
   alignment forced by the System.Text.Json bump). Sample-project
   transitives `SharpCompress` and `Snappier` are pinned to safe
   versions in `WidgetSample.csproj` and `Diagnostic.Service.csproj`.

After this cut, `dotnet list package --vulnerable
--include-transitive` reports **no vulnerable packages** for any
project in `DiagnosticExplorer.sln`.

## Why a new version instead of another `3.1.36` recut

The recut `3.1.36` (`b328538`) had the right API surface but couldn't
be adopted in practice — NuGet caches restored packages by version
under `~/.nuget/packages/<id>/<version>/`, so any machine that had
already restored the first (broken) `3.1.36` would continue to serve
that cached copy and ignore the recut nupkg in `LocalPackages`. The
BPX build failed with twelve `error CS1501: No overload for method
'AddDiagnosticExplorer' takes 1 arguments` errors against the recut,
because the resolved package was still the original broken `3.1.36`.

Bumping to `3.1.37` sidesteps this cleanly: a new version forces a
fresh restore on every machine, no cache cleanup required. Folding
the vulnerability fixes into the same bump avoids cutting a follow-up
`3.1.38` immediately afterwards.

## What this cut contains

Three source files in `DiagnosticExplorer.Hosting/` are at the
`3.1.35` shape (these landed via the earlier `4bf7fba` + `b328538`
recut commits on this branch, unchanged in this cut):

1. `DiagnosticHostingExtensions.cs` — `AddDiagnosticExplorer`
   requires `IConfiguration` and takes optional
   `Action<HttpConnectionOptions>`.
2. `DiagnosticHostingService.cs` — `_configureHttp` plumbed
   through constructors, `StartHosting`, and static
   `Start(string url, Action<HttpConnectionOptions> = null)`.
   Hosted-service registration uses a factory so the delegate
   reaches the registration handlers.
3. `RegistrationHandler.cs` — `Start(Action<HttpConnectionOptions>
   = null)` plumbs the delegate to `OpenHub`, with fall-back to
   the previous default-credentials lambda when no delegate is
   supplied.

Three csproj changes are new in this cut:

- `DiagnosticExplorer/DiagnosticExplorer.csproj` — version
  `3.1.37`, `log4net` `3.3.1`, `System.Text.Json` `8.0.5`.
- `DiagnosticExplorer.Hosting/DiagnosticExplorer.Hosting.csproj`
  — version `3.1.37`, `Microsoft.Bcl.AsyncInterfaces` `8.0.0`.
- `DiagnosticService/Diagnostic.Service.csproj` and
  `WidgetSample/WidgetSample.csproj` — `SharpCompress` `0.48.1`
  and `Snappier` `1.3.1` overrides on the transitive deps from
  `MongoDB.Driver`. Also `WidgetSample` `log4net` `3.3.1` direct
  pin update.

One source change is new in this cut:

- `DiagnosticService/Program.cs` — `AddDiagnosticExplorer()`
  changed to `AddDiagnosticExplorer(builder.Configuration)` so
  it compiles against the now-required-`IConfiguration` signature.

The lifecycle / race-condition fixes (`a8f0e78`, `83e675f`,
`f7b9957`, `b0c9343`, `b14bf44`) are unchanged. `DiagnosticResponse`
protobuf surface (restored `Events` and `Context`) is unchanged.

## API compatibility matrix

| Call form | 3.1.35 | first 3.1.36 | recut 3.1.36 | this 3.1.37 |
|---|---|---|---|---|
| `services.AddDiagnosticExplorer()` | compile error | works | compile error | compile error |
| `services.AddDiagnosticExplorer(config)` | works | compile error | works | works |
| `services.AddDiagnosticExplorer(config, configureHttp)` | works | compile error | works | works |
| `DiagnosticHostingService.Start(url)` | works | works | works | works |
| `DiagnosticHostingService.Start(url, configureHttp)` | works | compile error | works | works |

`3.1.37` matches `3.1.35` exactly for every call form. Consumers
upgrading from `3.1.35` need no source changes.

## Consumer migration

- **From `3.1.35`**: bump the pinned version in `Directory.Packages.props`
  (or the equivalent), drop the new nupkg into `LocalPackages` (or
  the equivalent), restore, rebuild. No source changes.
- **From the first `3.1.36`** (the broken cut at `f6a2228`): same as
  above, plus update any `services.AddDiagnosticExplorer()` call
  sites to pass `IConfiguration` explicitly. The downstream BPX
  fix-up in commit `60c297c42` of `ems-win-app.worktrees\BPX` shows
  the pattern.
- **From the recut `3.1.36`** (`b328538` on this branch): same
  package-bump step; no source changes if the BPX fix-up already
  landed.
