# 3.1.36 recut — rationale

This document explains why `3.1.36` of `DiagnosticExplorer` and
`DiagnosticExplorer.Hosting` was repackaged on the
`anvil/fix-tracescope-lifecycle` branch after the initial `3.1.36`
cut (commit `f6a2228`). It is intended for the maintainer reviewing
the recut commit.

## TL;DR

The initial `3.1.36` packages silently removed two optional parameters
from the public hosting API that downstream consumers depend on. The
removal was unrelated to the lifecycle bug fixes that motivated the
version bump from `3.1.35`. This recut restores the parameters
additively, so consumers can move from `3.1.35` to `3.1.36` purely to
pick up the lifecycle fixes, without any source change at their call
sites.

## What was wrong with the initial 3.1.36

Between `3.1.35` and the first build of `3.1.36`, the following
public surface was removed:

```csharp
// 3.1.35 (and earlier)
public static IServiceCollection AddDiagnosticExplorer(
    this IServiceCollection services,
    IConfiguration config,
    Action<HttpConnectionOptions> configureHttp = null);

public static void DiagnosticHostingService.Start(
    string url,
    Action<HttpConnectionOptions> configureHttp = null);

public void RegistrationHandler.Start(
    Action<HttpConnectionOptions> configureHttp = null);
```

```csharp
// First 3.1.36 cut (commit f6a2228)
public static IServiceCollection AddDiagnosticExplorer(
    this IServiceCollection services);

public static void DiagnosticHostingService.Start(string url);

public void RegistrationHandler.Start();
```

The `Action<HttpConnectionOptions>` plumbing through
`DiagnosticHostingService` and `RegistrationHandler` was deleted in
the same change, and `RegistrationHandler.OpenHub` was hard-coded to
`options.UseDefaultCredentials = true`.

### Why this matters

The `configureHttp` hook is how a consumer configures the SignalR
HTTP connection back to the diagnostic server. Inside the EMS
codebase it is used to attach an Azure AD bearer token via
`HttpConnectionOptions.AccessTokenProvider`:

```csharp
// EmsFramework.cs
DiagnosticHostingService.Start(
    diagnosticUrl,
    config => config.AccessTokenProvider = GetCurrentAccessToken);
```

The first `3.1.36` cut not only breaks this call at compile time —
it removes the underlying mechanism entirely. The hard-coded
`UseDefaultCredentials = true` lambda inside `RegistrationHandler`
means that even after a consumer rewrites their call site to match
the new signature, the diagnostic uplink can no longer authenticate
in any environment that does not use Windows auth.

This removal was independent of the lifecycle / race-condition
fixes that justify the `3.1.35 → 3.1.36` bump (subscription restart
races, stale callback rejection, event-stream lifecycle, hub-response
fire-and-forget). There is no functional reason for the two changes
to ship together.

## What this recut does

Three source files in `DiagnosticExplorer.Hosting/` are amended back
to the `3.1.35` shape.

1. `DiagnosticHostingExtensions.cs` — restore `IConfiguration` as a
   required parameter on `AddDiagnosticExplorer` and
   `Action<HttpConnectionOptions>` as an optional one. The first
   `3.1.36` cut had dropped both and resolved `IConfiguration`
   internally via a temporary `services.BuildServiceProvider()`. That
   pattern is an antipattern (ASP0000) — it instantiates and disposes
   every singleton registered so far, which then get rebuilt in the
   real container — so it is not preserved here.

2. `DiagnosticHostingService.cs` — restore the `_configureHttp` field
   and the optional `Action<HttpConnectionOptions>` parameter on the
   instance and `IOptions` constructors, on `StartHosting`, and on the
   static `Start(string url, ...)` overload. The instance is now
   registered via a factory in `AddDiagnosticExplorer` so the
   delegate flows from the extension method through the hosted
   service to the registration handlers.

3. `RegistrationHandler.cs` — restore `Start(Action<HttpConnectionOptions> = null)`
   and use the supplied delegate in `OpenHub` when non-null, falling
   back to the existing default-credentials lambda otherwise.

The lifecycle / race-condition fixes from `a8f0e78`, `83e675f`,
`f7b9957`, `b0c9343`, and the `TraceScope` async-stack rewrite from
`b14bf44`, are unchanged. The `DiagnosticResponse` protobuf surface
(restored `Events` and `Context` members) is unchanged.

## API compatibility matrix

| Call form | 3.1.35 | first 3.1.36 (f6a2228) | this recut |
|---|---|---|---|
| `services.AddDiagnosticExplorer()` | compile error | works | **compile error** |
| `services.AddDiagnosticExplorer(config)` | works | compile error | works |
| `services.AddDiagnosticExplorer(config, configureHttp)` | works | compile error | works |
| `DiagnosticHostingService.Start(url)` | works | works | works |
| `DiagnosticHostingService.Start(url, configureHttp)` | works | compile error | works |

The no-arg form introduced by the first `3.1.36` is deliberately
dropped. It was an antipattern (see file note above) and matched no
working consumer call site — the two BPX places that used it
(`Test.ConsoleApp/Program.cs`, `JobsService/Program.cs`) also failed
against `3.1.35`, so they were already broken or excluded from build
and were updated in the same change-set to pass `IConfiguration`
explicitly. Every other call form that compiled against `3.1.35`
compiles unchanged against this recut.

## Verification

Built `DiagnosticExplorer.Hosting` cleanly across all three target
frameworks (`net8.0`, `net6.0`, `net48`). Decompiled the resulting
`net8.0` assembly and confirmed the public surface matches the
intended signatures. Packages re-generated into `nupkg/` and pinned
at `3.1.36`.

## Consumer migration

Consumers on `3.1.35` upgrading to this recut of `3.1.36` should
require no source changes. Consumers who already adopted the first
`3.1.36` and rewrote their call sites to the no-arg form also
require no further changes — the no-arg overload remains.
