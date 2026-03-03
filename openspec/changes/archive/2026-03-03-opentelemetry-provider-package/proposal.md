## Why

Fulora has a built-in `ITelemetryProvider` and `IBridgeTracer` for telemetry and bridge call tracing. However, exporting this data to production observability backends (Jaeger, Grafana, Datadog, Azure Monitor) requires a manual OpenTelemetry integration. Enterprise teams expect a drop-in NuGet package that bridges Fulora's telemetry to the OpenTelemetry SDK standard. Without this, teams must write custom glue code or forego production observability.

**Goal alignment**: Phase 10 M10.3 (OpenTelemetry Provider Package). Directly supports production deployment readiness for enterprise adopters.

## What Changes

- Create a new NuGet package `Agibuild.Fulora.Telemetry.OpenTelemetry` (separate project)
- Implement `OpenTelemetryTelemetryProvider : ITelemetryProvider` that maps `TrackEvent` → OTLP spans, `TrackMetric` → OTLP metrics, `TrackException` → OTLP exception events
- Implement `OpenTelemetryBridgeTracer : IBridgeTracer` that creates Activity spans for bridge call start/end/error with service name, method name, and duration as span attributes
- Provide `FuloraServiceBuilder.AddOpenTelemetry()` extension for one-liner DI registration
- Configure `ActivitySource` named `Agibuild.Fulora` for bridge call tracing
- Add `Meter` named `Agibuild.Fulora` for bridge call metrics (call count, latency histogram, error rate)
- Include integration tests using in-memory OTLP exporter

## Capabilities

### New Capabilities
- `opentelemetry-bridge-provider`: OpenTelemetry SDK integration for ITelemetryProvider and IBridgeTracer with Activity-based span emission and Meter-based metrics

### Modified Capabilities
- `webview-di-integration`: Add `AddOpenTelemetry()` builder method to FuloraServiceBuilder

## Non-goals

- Sentry integration — separate package (`Agibuild.Fulora.Telemetry.Sentry`), different change
- Custom OTLP exporter — users configure exporters via standard OpenTelemetry SDK configuration
- Distributed tracing across WebView boundary — bridge calls are local, not cross-process
- Auto-instrumentation of non-bridge calls (navigation, resource loading)

## Impact

- New project: `src/Agibuild.Fulora.Telemetry.OpenTelemetry/`
- Modified: `src/Agibuild.Fulora.DependencyInjection/ServiceCollectionExtensions.cs` (AddOpenTelemetry extension)
- New test project: `tests/Agibuild.Fulora.Telemetry.OpenTelemetry.Tests/`
- Dependencies: `OpenTelemetry.Api` (stable), `OpenTelemetry.Extensions.Hosting`
- NuGet publish: new package in release pipeline
