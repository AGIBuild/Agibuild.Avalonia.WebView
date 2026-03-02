## Why

Production apps need observability: telemetry for usage and performance, crash reporting for stability. Fulora has no built-in telemetry or crash reporting. Developers must integrate OpenTelemetry, Sentry, or similar ad hoc. Bridge call metrics (latency, errors) are valuable for debugging but not auto-collected. An `ITelemetryProvider` interface with bridge-integrated metrics and built-in OpenTelemetry/Sentry providers would standardize this.

**Goal alignment**: Enable production observability; auto-collect bridge call metrics; support OpenTelemetry and Sentry as first-class providers; keep integration opt-in.

## What Changes

- Add `ITelemetryProvider` interface with methods for events, metrics, exceptions
- Auto-collect bridge call metrics (latency, success/failure) when a telemetry provider is registered
- Provide built-in providers: OpenTelemetry (traces, metrics export) and Sentry (crash reporting, error tracking)
- Bridge-integrated: optional JS API to report custom events or errors to the provider
- Document setup and provider configuration

## Non-goals

- Mandatory telemetry (opt-in only)
- Replacing or competing with OpenTelemetry/Sentry; integrate with them
- Full APM (application performance monitoring) — focus on bridge metrics and crash reporting

## Capabilities

### New Capabilities
- `telemetry-integration`: ITelemetryProvider interface with bridge call metrics auto-collection and built-in OpenTelemetry/Sentry providers

## Impact

- New package or extension: `Agibuild.Fulora.Telemetry` (or integrated into runtime)
- Bridge: Optional telemetry API for JS; auto-instrumentation of bridge calls
- NuGet: Optional packages for OpenTelemetry and Sentry integrations
- Documentation: Telemetry setup, provider configuration, privacy considerations
