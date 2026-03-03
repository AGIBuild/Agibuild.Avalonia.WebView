# OpenTelemetry Provider Package — Tasks

## 1. Project Setup

- [x] 1.1 Create new project `src/Agibuild.Fulora.Telemetry.OpenTelemetry/Agibuild.Fulora.Telemetry.OpenTelemetry.csproj`
- [x] 1.2 Add package references: `OpenTelemetry.Api` (stable), `Agibuild.Fulora` (core)
- [x] 1.3 Add project reference to `Agibuild.Fulora.DependencyInjection` for `FuloraServiceBuilder` extension
- [x] 1.4 Configure package metadata: Id, Version, Authors, Description, License
- [x] 1.5 Set target frameworks (net8.0 or per-solution standard)

## 2. OpenTelemetryBridgeTracer Implementation

- [x] 2.1 Create `OpenTelemetryBridgeTracer` class implementing `IBridgeTracer`
- [x] 2.2 Instantiate `ActivitySource` with name `Agibuild.Fulora` and version from assembly
- [x] 2.3 Implement `OnExportCallStart` / `OnImportCallStart` → start Activity, store in context/AsyncLocal
- [x] 2.4 Implement `OnExportCallEnd` / `OnImportCallEnd` → stop Activity, record duration
- [x] 2.5 Implement `OnExportCallError` → stop Activity with Error status, record exception event
- [x] 2.6 Add span attributes: `fulora.service_name`, `fulora.method_name`, `fulora.direction`
- [x] 2.7 Instantiate `Meter` with name `Agibuild.Fulora`
- [x] 2.8 Create Counter `fulora.bridge.call_count`, Histogram `fulora.bridge.call_latency_ms`, Counter `fulora.bridge.call_errors`
- [x] 2.9 Emit metrics on call end/error with dimensions (service_name, method_name, direction, status)
- [x] 2.10 Implement `OnServiceExposed` / `OnServiceRemoved` (no-op or optional span; document behavior)

## 3. OpenTelemetryTelemetryProvider Implementation

- [x] 3.1 Create `OpenTelemetryTelemetryProvider` class implementing `ITelemetryProvider`
- [x] 3.2 Implement `TrackEvent` → create and end Activity span with event name and properties as attributes
- [x] 3.3 Implement `TrackMetric` → record via Meter (histogram or counter based on semantics)
- [x] 3.4 Implement `TrackException` → record exception event on current Activity or new span
- [x] 3.5 Implement `Flush` → call OpenTelemetry `ForceFlush` when available; no-op otherwise
- [x] 3.6 Reuse or share ActivitySource/Meter with `OpenTelemetryBridgeTracer` for consistency

## 4. DI Integration (AddOpenTelemetry Extension)

- [x] 4.1 Create extension method `FuloraServiceBuilder.AddOpenTelemetry()` in `Agibuild.Fulora.DependencyInjection` or in the OpenTelemetry package with reference to DI
- [x] 4.2 Register `OpenTelemetryTelemetryProvider` as `ITelemetryProvider`
- [x] 4.3 Register `OpenTelemetryBridgeTracer` as `IBridgeTracer`
- [x] 4.4 Ensure single ActivitySource and Meter instance shared by both implementations
- [x] 4.5 Add XML docs and usage example in README or package description

## 5. Tests (In-Memory Exporter Validation)

- [x] 5.1 Create test project `tests/Agibuild.Fulora.Telemetry.OpenTelemetry.Tests/`
- [x] 5.2 Add `OpenTelemetry` test packages (e.g., `OpenTelemetry.Instrumentation` or in-memory exporter)
- [x] 5.3 Test `OpenTelemetryBridgeTracer`: export/import call start→end produces one span each
- [x] 5.4 Test `OpenTelemetryBridgeTracer`: `OnExportCallError` produces span with Error status and exception event
- [x] 5.5 Test span attributes: service_name, method_name, direction, duration
- [x] 5.6 Test Meter: call_count, latency histogram, error counter emitted with correct dimensions
- [x] 5.7 Test `OpenTelemetryTelemetryProvider`: TrackEvent, TrackMetric, TrackException produce expected signals
- [x] 5.8 Test `AddOpenTelemetry()`: both provider and tracer registered, integration with bridge works end-to-end

## 6. Package Metadata & CI

- [x] 6.1 Set NuGet package ID: `Agibuild.Fulora.Telemetry.OpenTelemetry`
- [x] 6.2 Add package to solution and ensure it builds in CI
- [x] 6.3 Add package to release/publish pipeline (if applicable)
- [x] 6.4 Document setup: add package, call `AddOpenTelemetry()`, configure OTLP exporter in host
