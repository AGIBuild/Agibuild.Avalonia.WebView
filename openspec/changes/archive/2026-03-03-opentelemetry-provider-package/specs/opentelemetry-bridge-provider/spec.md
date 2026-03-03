## Purpose

Define BDD-style requirements for the OpenTelemetry provider package: `OpenTelemetryBridgeTracer`, `OpenTelemetryTelemetryProvider`, and `AddOpenTelemetry()` DI integration. Ensures bridge calls and telemetry events are correctly mapped to OTLP spans and metrics.

## Requirements

### Requirement: OpenTelemetryBridgeTracer creates Activity spans per bridge call

`OpenTelemetryBridgeTracer` SHALL implement `IBridgeTracer` and SHALL create one Activity span per bridge call (export or import).

#### Scenario: Export call creates span from start to end
- **GIVEN** an `OpenTelemetryBridgeTracer` registered with an ActivitySource named `Agibuild.Fulora`
- **WHEN** `OnExportCallStart(serviceName, methodName, paramsJson)` is invoked
- **THEN** a new Activity SHALL be started with a span name derived from service and method
- **AND** when `OnExportCallEnd(serviceName, methodName, elapsedMs, resultType)` is invoked
- **THEN** the Activity SHALL be stopped
- **AND** the span SHALL be exported to any configured OTLP exporter

#### Scenario: Import call creates span from start to end
- **GIVEN** an `OpenTelemetryBridgeTracer` registered with an ActivitySource named `Agibuild.Fulora`
- **WHEN** `OnImportCallStart(serviceName, methodName, paramsJson)` is invoked
- **THEN** a new Activity SHALL be started
- **AND** when `OnImportCallEnd(serviceName, methodName, elapsedMs)` is invoked
- **THEN** the Activity SHALL be stopped

### Requirement: Span attributes include service name, method name, direction, duration

Each bridge call span SHALL include OTLP-compatible attributes.

#### Scenario: Span has required attributes
- **GIVEN** a bridge call span created by `OpenTelemetryBridgeTracer`
- **THEN** the span SHALL include attributes: `fulora.service_name`, `fulora.method_name`, `fulora.direction` (export|import)
- **AND** the span SHALL record duration from start to stop
- **AND** optional `fulora.params_json` MAY be set when params are provided (truncated if large)

### Requirement: Error spans have exception event attached

When a bridge call fails, the span SHALL record the error and exception.

#### Scenario: Export call error records exception on span
- **GIVEN** `OnExportCallStart` was invoked for a call
- **WHEN** `OnExportCallError(serviceName, methodName, elapsedMs, error)` is invoked
- **THEN** the Activity SHALL be stopped with status `Error`
- **AND** an exception event SHALL be recorded on the span with the exception type, message, and stack trace
- **AND** the span SHALL include an attribute indicating the call failed

### Requirement: Meter emits call count, latency histogram, error counter

`OpenTelemetryBridgeTracer` SHALL use a Meter named `Agibuild.Fulora` to emit metrics for bridge calls.

#### Scenario: Successful call emits count and latency
- **GIVEN** an `OpenTelemetryBridgeTracer` with Meter `Agibuild.Fulora`
- **WHEN** `OnExportCallEnd` or `OnImportCallEnd` is invoked for a successful call
- **THEN** a Counter `fulora.bridge.call_count` SHALL be incremented with dimensions `service_name`, `method_name`, `direction`, `status=ok`
- **AND** a Histogram `fulora.bridge.call_latency_ms` SHALL record `elapsedMs` with the same dimensions

#### Scenario: Failed call emits error counter
- **GIVEN** an `OpenTelemetryBridgeTracer` with Meter `Agibuild.Fulora`
- **WHEN** `OnExportCallError` is invoked
- **THEN** a Counter `fulora.bridge.call_errors` SHALL be incremented with dimensions `service_name`, `method_name`, `direction=export`
- **AND** `fulora.bridge.call_count` SHALL be incremented with `status=error`

### Requirement: OpenTelemetryTelemetryProvider maps to OTLP conventions

`OpenTelemetryTelemetryProvider` SHALL implement `ITelemetryProvider` and SHALL map events, metrics, and exceptions to OTLP-compatible signals.

#### Scenario: TrackEvent produces span or event
- **GIVEN** an `OpenTelemetryTelemetryProvider` configured with ActivitySource
- **WHEN** `TrackEvent(name, properties)` is invoked
- **THEN** an Activity span SHALL be created with span name equal to `name`
- **AND** each property in `properties` SHALL be added as a span attribute
- **AND** the span SHALL be ended immediately (zero duration event)

#### Scenario: TrackMetric produces metric
- **GIVEN** an `OpenTelemetryTelemetryProvider` configured with Meter
- **WHEN** `TrackMetric(name, value, dimensions)` is invoked
- **THEN** a metric SHALL be recorded with instrument name derived from `name`
- **AND** dimensions SHALL be used as metric attributes

#### Scenario: TrackException produces exception event
- **GIVEN** an `OpenTelemetryTelemetryProvider`
- **WHEN** `TrackException(exception, properties)` is invoked
- **THEN** an exception event SHALL be recorded (on current Activity if present, or a new span)
- **AND** exception type, message, and stack trace SHALL be included per OTLP exception conventions

#### Scenario: Flush delegates to OpenTelemetry
- **GIVEN** an `OpenTelemetryTelemetryProvider`
- **WHEN** `Flush()` is invoked
- **THEN** the provider SHALL attempt to flush any buffered telemetry (e.g., via OpenTelemetry SDK `ForceFlush` if configured)
- **AND** SHALL complete without throwing when no SDK is configured

### Requirement: DI registration via AddOpenTelemetry

`FuloraServiceBuilder` SHALL support `AddOpenTelemetry()` extension for one-liner registration.

#### Scenario: AddOpenTelemetry registers provider and tracer
- **GIVEN** a `FuloraServiceBuilder` (e.g., from `services.AddFulora()`)
- **WHEN** `AddOpenTelemetry()` is invoked
- **THEN** `OpenTelemetryTelemetryProvider` SHALL be registered as `ITelemetryProvider`
- **AND** `OpenTelemetryBridgeTracer` SHALL be registered as `IBridgeTracer`
- **AND** both SHALL use the same ActivitySource and Meter (`Agibuild.Fulora`)

#### Scenario: AddOpenTelemetry is composable with other builder methods
- **GIVEN** a builder chain: `services.AddFulora().AddJsonFileConfig(path).AddOpenTelemetry()`
- **WHEN** the application configures services
- **THEN** telemetry and bridge tracer SHALL be registered alongside other Fulora services
- **AND** no conflict SHALL occur with `AddTelemetry(provider)` if both are used (last registration wins per DI rules)
