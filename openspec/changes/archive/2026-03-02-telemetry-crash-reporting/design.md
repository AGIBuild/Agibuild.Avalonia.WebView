## Context

Fulora's bridge handles C# ↔ JS RPC. Each call has latency, success/failure, and error details. These are valuable for production diagnostics but not currently collected. OpenTelemetry and Sentry are industry-standard for traces/metrics and crash reporting. An abstraction allows apps to plug in their preferred backend while Fulora provides auto-collection of bridge metrics.

**Gap**: No `ITelemetryProvider`; no auto-collection of bridge metrics; no built-in Sentry/OpenTelemetry integration.

## Goals / Non-Goals

**Goals:**
- `ITelemetryProvider` with TrackEvent, TrackMetric, TrackException
- Auto-collect bridge call metrics (service, method, latency, success/error) when provider registered
- Built-in OpenTelemetry provider (export traces/metrics to OTLP or configured exporter)
- Built-in Sentry provider (capture exceptions, optional breadcrumbs)
- Optional JS API to report custom events/errors to provider

**Non-Goals:**
- Mandatory telemetry; must be opt-in
- Full APM; focus on bridge + crash
- Custom backend implementations (only OpenTelemetry and Sentry as built-ins)

## Decisions

### D1: Provider registration

**Choice**: Register `ITelemetryProvider` via DI or WebView environment options. When registered, bridge middleware (or equivalent) instruments each call and reports to provider. No provider = no instrumentation.

**Rationale**: Opt-in; no overhead when disabled.

### D2: Bridge metrics schema

**Choice**: Each bridge call produces: service name, method name, direction (export/import), latency ms, success/failure, error message (if failed). Map to OpenTelemetry span or Sentry breadcrumb/span.

**Rationale**: Minimal, useful schema; compatible with OTel and Sentry models.

### D3: JS API scope

**Choice**: Optional `telemetry.trackEvent(name, properties)`, `telemetry.trackException(message, stack)`. JS events flow to C# provider. Not required for core telemetry (C#-side bridge metrics are primary).

**Rationale**: Allows frontend to report custom events (e.g., button clicks) or JS errors. Kept optional.

## Risks / Trade-offs

- **[Risk] PII in telemetry** → Document that apps must not include PII in event properties; provider is app responsibility.
- **[Risk] Performance overhead** → Instrumentation adds minimal overhead; async flush. Document.
- **[Trade-off] Built-in providers only** → Third-party providers can implement `ITelemetryProvider`; we ship OTel and Sentry.
