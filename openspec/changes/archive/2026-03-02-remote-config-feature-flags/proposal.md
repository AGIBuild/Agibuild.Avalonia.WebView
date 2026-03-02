## Why

Apps often need remote configuration (e.g., API endpoints, feature flags, A/B test variants) that can change without redeployment. Fulora has no built-in config provider. Developers must implement their own HTTP fetch + fallback, and JS cannot easily access config without bridge round-trips. A first-class `IConfigProvider` with local JSON fallback and optional remote HTTP endpoint would standardize this and allow bridge-integrated JS access.

**Goal alignment**: Enable remote config and feature flags without custom infrastructure; support offline fallback; expose config to JS via bridge for frontend-driven feature toggles.

## What Changes

- Add `IConfigProvider` interface with `GetValue<T>(key)`, `GetSection(key)`, and async refresh
- Provide built-in implementations: `JsonFileConfigProvider` (local JSON fallback), `RemoteConfigProvider` (HTTP endpoint with local fallback)
- Support merging: remote config overrides local when available; fallback to local when offline or on error
- Bridge-integrated: expose config to JS via `window.agWebView.config` (or equivalent) so frontend can read feature flags and config values
- Document usage and configuration

## Non-goals

- Full configuration framework (e.g., Microsoft.Extensions.Configuration replacement)
- Real-time config push (polling or on-demand refresh only)
- Config encryption or secret management (values are assumed non-sensitive or handled by app)

## Capabilities

### New Capabilities
- `remote-configuration`: IConfigProvider interface with local JSON fallback, optional remote HTTP endpoint, and bridge-integrated JS access

## Impact

- New package or extension: `Agibuild.Fulora.Config` (or integrated into runtime)
- Bridge: New `config` namespace for JS to read values
- Documentation: Config provider setup, remote endpoint format, feature flag usage
