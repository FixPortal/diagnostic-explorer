# DiagnosticService hub authentication & CORS — design proposal (opt-in)

Status: **proposal** (not yet implemented). Raised by the adversarial-review audit
(2026-05-29) as findings **H1** (hubs expose mutating/reflective operations with no
authentication) and **H2** (CORS reflects any origin with credentials).

## Constraint

The fix **must not break existing trusted-network deployments** or the EMS consumer
flow. Diagnostic clients (`DiagnosticExplorer.Hosting.RegistrationHandler`) and the
Angular SPA currently connect with no credentials. So authentication is introduced as
**opt-in**: the default configuration preserves today's open behaviour, and an operator
turns it on once clients are ready.

## Current state

- `Program.cs` maps `WebHub` (`/web-hub`) and `DiagnosticHub` (`/diagnostics`) with no
  `app.UseAuthentication()/UseAuthorization()` and no `[Authorize]`.
- `WebHub` exposes `RemoveProcess`, `SetProperty`, `ExecuteOperation`, `RetroDelete`;
  `DiagnosticHub` exposes `Register`, `LogEvents`. `SetProperty`/`ExecuteOperation`
  drive reflective property-set / method-invoke against live monitored objects
  (confirmed in the `lib-core` chunk), so an unauthenticated caller has broad control.
- CORS middleware uses `SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials()`.

## Proposed design

### 1. Configuration (`DiagServiceSettings`)

```jsonc
"DiagServiceSettings": {
  "Security": {
    "AuthMode": "None",            // None (default, == today) | ApiKey
    "ApiKeys": [],                 // accepted keys when AuthMode == ApiKey
    "AllowedCorsOrigins": []       // when empty: today's permissive CORS (+ startup warning)
  }
}
```

- `AuthMode: None` (default) → no behavioural change. Existing trusted-network
  deployments and EMS keep working untouched.
- `AuthMode: ApiKey` → every hub connection (and the negotiate request) must present a
  valid key.

### 2. API-key authentication (when `AuthMode == ApiKey`)

- Add a minimal API-key authentication handler (header `X-Diag-ApiKey` for HTTP, and
  the SignalR `access_token` query-string for the WebSocket upgrade, since browsers
  can't set headers on the WS handshake).
- Apply an `[Authorize]` policy to both hubs **only when `AuthMode != None`** — register
  the policy conditionally so the attribute is a no-op gate in `None` mode (or guard the
  `MapHub` calls with `.RequireAuthorization()` applied conditionally).
- Keys are compared with a fixed-time comparison; rotate by listing multiple keys.

### 3. CORS (H2)

- When `AllowedCorsOrigins` is non-empty: use `WithOrigins(origins).AllowCredentials()`
  — a real allowlist, no wildcard-with-credentials.
- When empty: keep today's permissive policy **but log a startup warning** that the
  service is accepting credentialed requests from any origin. This preserves current
  behaviour while making the risk visible.

### 4. Client plumbing

- `RegistrationHandler.OpenHub` already accepts a `configureHttp` delegate — add an
  optional API-key option to `DiagnosticOptions` and set the `access_token`/header there
  when configured. (Ties in with finding **M23**: make `UseDefaultCredentials` opt-in at
  the same time, so the default connection neither forwards Windows creds nor assumes a
  key unless configured.)
- The SPA reads its key from runtime config (`environment` / injected token) and passes
  it via `accessTokenFactory` on the `HubConnectionBuilder`.

## Rollout (phased, zero-break)

1. Ship the code with `AuthMode: None` — no behavioural change; verify nothing regresses.
2. Configure `ApiKeys` + update each diagnostic client / the SPA to send a key (clients
   keep working because the server is still in `None` mode).
3. Flip `AuthMode: ApiKey` once all clients send keys; set `AllowedCorsOrigins`.

## Why opt-in rather than mandatory

Making auth mandatory now would break every existing diagnostic client and the EMS
nupkg consumer flow at the moment of deploy. Opt-in lets a network-isolated deployment
stay as-is while an exposed one can be locked down without a flag-day migration.

## Implementation checklist (follow-up batch)

- [ ] `DiagServiceSettings.Security` POCO + binding + validation.
- [ ] API-key `AuthenticationHandler` (header + `access_token` query for WS).
- [ ] Conditional `UseAuthentication/UseAuthorization` + hub `RequireAuthorization`.
- [ ] CORS: allowlist when configured, warn-on-permissive otherwise.
- [ ] `DiagnosticOptions` API-key option + `RegistrationHandler` plumbing; make
      `UseDefaultCredentials` opt-in (M23).
- [ ] SPA `accessTokenFactory` + runtime config.
- [ ] Docs: operator guide for enabling auth + rotating keys.
