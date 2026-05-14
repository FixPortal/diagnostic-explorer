# DiagnosticExplorer

DiagnosticExplorer is a .NET diagnostic instrumentation toolkit and an
accompanying web-based viewer service. Application code emits property
bags, operations, and events via the `DiagnosticExplorer` library;
those get pushed over SignalR to a central hosting service which fans
them out to a browser-based dashboard for live inspection across a
fleet of running processes.

The project originated as Cameron Elliot's open-source diagnostic
toolset around 2010 (LGPL v3+) and has been carried forward under
Centerprise's EMS trading platform as the diagnostic backbone for the
TOMI engine and its surrounding services.

## Repository layout

```
DiagnosticExplorer/          netstandard2.0 core library
                             - PropertyBag, TraceScope, OperationSet,
                               protobuf transport types, log4net forwarding
DiagnosticExplorer.Hosting/  net8.0 / net6.0 / net48 hosting integration
                             - AddDiagnosticExplorer DI extension,
                               DiagnosticHostingService, RegistrationHandler
DiagnosticService/           Standalone web service (Docker payload)
                             - ASP.NET Core + SignalR hubs + SPA host
diagnostics-web/             Angular 13 SPA (the dashboard UI)
Docker/                      Dockerfile and compose YAMLs for the service
nupkg/                       Pre-built NuGet packages for offline / private use
WidgetSample/                WinForms demo of the library
ConsoleApp/                  Smaller CLI demo
RECUT-3.1.37.md              Rationale for the current release cut
```

## Using the library

Add the package reference (or drop the prebuilt nupkg from `nupkg/`
into a `LocalPackages/` folder and point a `Directory.Packages.props`
at it):

```xml
<PackageReference Include="DiagnosticExplorer.Hosting" Version="3.1.37" />
```

Wire into a `Host.CreateDefaultBuilder` pipeline:

```csharp
services.AddDiagnosticExplorer(context.Configuration);
```

For SignalR connections that need custom configuration (e.g. an Azure
AD bearer token), pass an `Action<HttpConnectionOptions>`:

```csharp
services.AddDiagnosticExplorer(
    context.Configuration,
    options => options.AccessTokenProvider = GetCurrentAccessToken);
```

Static start (for non-DI hosts):

```csharp
DiagnosticHostingService.Start(
    "http://diagnostics:2803/diagnostics",
    options => options.AccessTokenProvider = GetCurrentAccessToken);
```

Required configuration:

```json
{
  "DiagnosticExplorer": {
    "Uri": "http://diagnostics:2803/diagnostics",
    "Enabled": true
  }
}
```

The `Uri` may be a comma-or-semicolon-separated list of hub URLs if you
want a single application to report to multiple diagnostic servers.

### Tracing scopes

```csharp
using (var scope = new TraceScope(Log.Info))
{
    TraceScope.Trace("Loaded {0} records", count);
    // ... work ...
    TraceScope.Trace("Completed in {0}ms", elapsed);
}
```

`TraceScope` flows through `AsyncLocal`, so nested async calls share
the same scope automatically.

## Running the service

Two compose files under `Docker/`:

```bash
# Build the image locally and bring up service + MongoDB:
docker compose -f Docker/compose-and-create-image.yaml up -d --build

# Or pull the published image from ghcr.io:
docker compose -f Docker/compose-with-existing-image.yaml up -d
```

Then open `http://localhost:2803/` for the dashboard.

Environment-variable overrides documented inline at the top of each
compose file. Most-useful:

| Variable | Default | Purpose |
|---|---|---|
| `DIAGEXPLORER_HOST_PORT` | `2803` | Host-side port mapping if 2803 is in use locally |
| `MONGO_USERNAME` / `MONGO_PASSWORD` | `admin` / `password123` | Mongo root credentials |
| `DIAGEXPLORER_IMAGE_TAG` | `latest` | Pin to a specific GHCR tag (e.g. `3.1.37`) |

The service listens on port `2803` inside the container. Settings
default to a Mongo backend at `mongodb:27017` (the sidecar in compose);
override `DiagServiceSettings__RetroConnection` to point at a
different store.

## Building from source

```bash
# Library + service + samples:
dotnet build DiagnosticExplorer.sln -c Release

# NuGet packages are auto-generated via <GeneratePackageOnBuild>true</>
# and land under each project's bin/Release/. They're then copied
# into nupkg/ at release time for the LocalPackages workflow.

# Angular dashboard (Node 16 recommended; Angular 13 doesn't love Node 18+):
cd diagnostics-web
npm ci --legacy-peer-deps
npm run build
```

Vulnerability check:

```bash
dotnet list DiagnosticExplorer.sln package --vulnerable --include-transitive
```

(Should report no vulnerable packages as of `3.1.37`.)

## Container image

Published to `ghcr.io/cell001nz/diagnostic-explorer` by the GitHub
Actions workflow at `.github/workflows/publish-docker-image.yml`.

Triggers:
- `push` to `main` → tags `latest`, `main`, `sha-<short>`
- `push` of a `v*.*.*` git tag → tags the matching semver + `major.minor`
- `workflow_dispatch` → manual publish from any ref
- `pull_request` against `main` → build-validate only, no push

The image is `linux/amd64`. First-publish caveat: the GHCR package
defaults to private — flip it to public from the package's GitHub
Settings page if you want anonymous pulls.

## Releases

Current release: **3.1.37**.

See `RECUT-3.1.37.md` for the rationale behind the `3.1.37` cut,
including:
- the broken `3.1.36` cuts it supersedes,
- the `AddDiagnosticExplorer` API surface restoration,
- and the vulnerability bumps for `log4net`, `System.Text.Json`, and
  `Microsoft.Bcl.AsyncInterfaces`.

Versions are tagged with `v{semver}` (e.g. `v3.1.37`); pushing the tag
triggers an image publish to GHCR.

## License

LGPL v3 or later — see `LICENSE` and the file headers in
`DiagnosticExplorer/`.
