# Angular upgrade, test modernization, and mutation CI design

## Summary

Refresh `diagnostics-web` from Angular 13 to the current Angular release, modernize the frontend unit-test stack, replace low-value generated specs with behavior-focused tests, and add Stryker-based mutation analysis to CI using compact summary artifacts.

## Current context

- `diagnostics-web` is an Angular 13 application with Angular Material 12-era dependencies and a legacy CLI-generated Karma/Jasmine setup.
- The repository-level consumers of the SPA are the Docker build in `Docker\Dockerfile`, the PR container-validation flow in `.github\workflows\publish-docker-image.yml`, the build/run documentation in `README.md`, and the debug SPA path in `DiagnosticService\Config\settings.Debug.json`.
- Existing spec files are present, but many are boilerplate smoke tests that assert little beyond component creation.
- The most behavior-rich frontend seams live in `DiagHubService`, `RealtimeModel`, `RetroModel`, filtering/pipes/utilities, and a handful of components with state transitions or emitted events.

## Goals

1. Upgrade the Angular application to the latest Angular release while preserving existing runtime behavior unless a framework change forces an intentional adjustment.
2. Replace the legacy frontend unit-test foundation with a maintainable modern setup that supports comprehensive behavior-focused tests.
3. Build a real test suite around the application's high-value logic instead of inflating shallow coverage.
4. Add Stryker mutation analysis to CI and publish compact machine-readable and markdown summary artifacts alongside the full report.

## Non-goals

- Broad UX redesign or feature work unrelated to the Angular upgrade and test hardening.
- Blanket rewrites of thin display-only components that do not justify deeper test investment.
- Tight mutation thresholds before the first meaningful baseline exists.

## Recommended approach

Use an incremental framework upgrade with controlled checkpoints, modernize the test stack during the refresh, then add mutation testing once the real unit suite is stable.

This balances delivery risk better than a big-bang rewrite and avoids investing heavily in the outgoing Angular 13/Karma baseline.

## Delivery shape

### Slice A: Framework refresh

- Upgrade `diagnostics-web` through supported Angular major steps until it reaches the current Angular release.
- Update Angular CLI/build configuration, Angular Material/CDK dependencies, and any required application code or configuration changes introduced by framework migrations.
- Keep the application buildable at each major step before proceeding.

### Slice B: Test-stack modernization and suite replacement

- Replace the legacy generated Karma/Jasmine-centric testing setup with a modern frontend unit-test stack suitable for current Angular and long-term maintenance.
- Remove or rewrite low-value smoke specs that only assert component creation or CLI placeholder text.
- Build behavior-focused tests for:
  - `DiagHubService`
  - `RealtimeModel`
  - `RetroModel`
  - pipes and utility logic
  - components that emit events, react to input changes, or coordinate model state
- Keep thin display wrappers on lightweight smoke/input-binding coverage unless the upgrade introduces meaningful new behavior there.

### Slice C: Repo integration and mutation CI

- Update the repo-level SPA consumers that depend on the Angular build output or toolchain:
  - `Docker\Dockerfile`
  - `.github\workflows\publish-docker-image.yml`
  - `README.md`
  - `DiagnosticService\Config\settings.Debug.json`
- Add a dedicated mutation workflow for the Angular app.
- Publish:
  - the full Stryker report
  - a compact JSON mutation summary artifact
  - a markdown mutation summary suitable for the GitHub step summary

## Testing strategy

### Priority targets

The comprehensive suite should emphasize behavior-heavy seams:

- **SignalR/service behavior**: connection lifecycle, reconnect handling, request dispatch, and error paths in `DiagHubService`
- **Realtime state management**: process filtering, selection, message state, event ingestion, and category updates in `RealtimeModel`
- **Retro search state management**: query creation, cancellation, result accumulation, filtering, completion states, and deletion flows in `RetroModel`
- **Focused UI logic**: components such as `EventFilterComponent` and similar interaction-heavy components where outputs and state changes matter
- **Pure transforms**: pipes and utility helpers where mutation testing is especially effective

### Lower-priority targets

Thin view wrappers should receive only enough coverage to protect wiring and rendering assumptions, not deep test scaffolding for behavior they do not own.

## Mutation-hardening strategy

- Introduce Stryker only after the upgraded app and the modernized unit suite are stable.
- Start from a permissive baseline threshold so the first run exposes weak assertions instead of blocking adoption.
- Use surviving mutants to drive targeted hardening in the highest-value files first.
- Tighten thresholds later without redesigning the workflow structure.
- Preserve the established FixPortal pattern of publishing compact, machine-readable mutation summaries rather than relying on manual inspection of the full HTML report.

## CI design

### Frontend validation

Add explicit frontend validation so failures show up before the Docker build fails indirectly:

- dependency restore/install
- Angular build
- modernized unit-test execution

### Integration backstop

Keep the existing Docker publish/build workflow as an end-to-end validator for the SPA + service packaging path.

### Mutation workflow

Add a separate workflow that:

1. restores the frontend toolchain
2. runs Stryker against the upgraded Angular app
3. locates the latest Stryker report output
4. generates compact JSON and markdown summaries
5. uploads both the summaries and the full report as artifacts

## Error handling and safety model

- Each Angular major upgrade step must leave the app in a buildable state before the next step begins.
- The new test stack must be green before the legacy smoke suite is fully removed.
- Framework-induced failures should be resolved with targeted compatibility fixes, not speculative rewrites.
- Preserve current runtime behavior unless the upgrade requires a deliberate change that is documented in-code and in tests.
- Use both explicit frontend validation and the Docker build as complementary safety rails.

## Expected repository changes

- `diagnostics-web\package.json`
- `diagnostics-web\package-lock.json`
- `diagnostics-web\angular.json`
- `diagnostics-web\tsconfig*.json`
- frontend source files affected by Angular migrations or test modernization
- frontend spec files and test helpers
- Stryker configuration and summary script for the Angular app
- `.github\workflows\publish-docker-image.yml`
- new mutation workflow file
- `Docker\Dockerfile`
- `README.md`

## Open decisions already resolved

- **First priority**: Angular upgrade comes before broad test hardening.
- **Refresh scope**: include the broader frontend-platform refresh in the first step.
- **Test direction**: modernize the frontend test setup rather than preserving the legacy Karma/Jasmine baseline as-is.
