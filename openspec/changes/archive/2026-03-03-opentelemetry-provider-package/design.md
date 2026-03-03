## Context

Fulora exposes `ITelemetryProvider` and `IBridgeTracer` for telemetry and bridge call tracing. Production observability backends (Jaeger, Grafana, Datadog, Azure Monitor) expect OTLP-compatible data. Without a dedicated integration package, teams must write custom glue code to bridge Fulora's telemetry to the OpenTelemetry SDK. This change introduces a drop-in NuGet package that maps Fulora's interfaces to OpenTelemetry conventions.

**Existing contracts**: `ITelemetryProvider` (TrackEvent, TrackMetric, TrackException, Flush) and `IBridgeTracer` (OnExportCallStart/End/Error, OnImportCallStart/End, OnServiceExposed/Removed). DI uses `FuloraServiceBuilder.AddTelemetry(provider)`.

## Goals / Non-Goals

**Goals:**
- New NuGet package `Agibuild.Fulora.Telemetry.OpenTelemetry` separate from core
- `OpenTelemetryTelemetryProvider : ITelemetryProvider` mapping to OTLP spans/metrics
- `OpenTelemetryBridgeTracer : IBridgeTracer` mapping to Activity spans with attributes
- `FuloraServiceBuilder.AddOpenTelemetry()` extension for one-liner DI registration
- ActivitySource `Agibuild.Fulora` for bridge call tracing
- Meter `Agibuild.Fulora` for metrics (call count, latency histogram, error rate)

**Non-Goals:**
- Sentry integration (separate package)
- Custom OTLP exporter configuration (users configure via standard OpenTelemetry SDK)
- Distributed tracing across WebView boundary (bridge calls are local)
- Auto-instrumentation of non-bridge calls (navigation, resource loading)

## Decisions

### D1: Separate NuGet package (not in core)

**Choice**: Create `Agibuild.Fulora.Telemetry.OpenTelemetry` as a standalone package. Core `Agibuild.Fulora` does not reference OpenTelemetry SDK.

**Rationale**: Keeps core lightweight; avoids forcing OpenTelemetry dependency on all consumers. Teams that need OTLP export opt in by adding the package.

### D2: ActivitySource-based tracing (System.Diagnostics.Activity)

**Choice**: Use `ActivitySource` and `Activity` for bridge call spans. Each `OnExportCallStart` / `OnImportCallStart` creates an Activity; `OnExportCallEnd` / `OnImportCallEnd` / `OnExportCallError` stops it.

**Rationale**: Aligns with OpenTelemetry .NET conventions; `Activity` is the standard abstraction for spans. OTLP exporters consume Activity automatically.

### D3: Meter-based metrics with specific instrument names

**Choice**: Use `Meter` with instruments: `fulora.bridge.call_count` (Counter), `fulora.bridge.call_latency_ms` (Histogram), `fulora.bridge.call_errors` (Counter). Dimensions: `service_name`, `method_name`, `direction` (export/import), `status` (ok/error).

**Rationale**: Semantic instrument names and dimensions enable dashboards and alerting in OTLP-compatible backends.

### D4: IBridgeTracer events map to Activity start/stop semantics

**Choice**: `OnExportCallStart` / `OnImportCallStart` → `Activity.StartActivity()`; `OnExportCallEnd` / `OnImportCallEnd` → `Activity.Stop()`; `OnExportCallError` → stop with `Activity.SetStatus(ActivityStatusCode.Error)` and record exception event.

**Rationale**: One-to-one mapping preserves call boundaries and error context.

### D5: ITelemetryProvider events map to OTLP conventions

**Choice**: `TrackEvent` → span with event name as span name, properties as attributes; `TrackMetric` → Meter histogram or counter; `TrackException` → exception event on current span or new span; `Flush` → delegate to OpenTelemetry `ForceFlush` if available.

**Rationale**: OTLP semantic conventions ensure compatibility with standard backends.

### D6: Depends on OpenTelemetry.Api (stable) only, not full SDK

**Choice**: Package references `OpenTelemetry.Api` for `ActivitySource`, `Meter`, `Activity`. Exporters and SDK configuration are the host application's responsibility.

**Rationale**: Minimal dependency surface; host adds `OpenTelemetry.Extensions.Hosting` and exporter packages as needed.

## Risks / Trade-offs

- **[Risk] Host must configure exporters** → Package emits data via Activity/Meter; if host does not add OTLP exporter, data is dropped. Document setup steps.
- **[Risk] ActivitySource/Meter naming** → Fixed name `Agibuild.Fulora`; versioning via package version. Document for backend filtering.
- **[Trade-off] No built-in exporter** → Keeps package small; users add `OpenTelemetry.Exporter.OpenTelemetryProtocol` or similar.
- **[Trade-off] Flush semantics** → `ITelemetryProvider.Flush` may not map to OpenTelemetry `ForceFlush` if SDK not configured; document best-effort behavior.
