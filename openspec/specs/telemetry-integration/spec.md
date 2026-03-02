## Purpose

Define structured telemetry and crash reporting integration for Agibuild.Fulora, with ITelemetryProvider interface, bridge call metrics auto-collection, and built-in OpenTelemetry and Sentry providers.

## ADDED Requirements

### Requirement: ITelemetryProvider interface
Runtime SHALL provide an `ITelemetryProvider` interface with methods for events, metrics, and exceptions.

#### Scenario: TrackEvent records custom event
- **WHEN** C# or JS calls `TrackEvent(name, properties)` with event name and optional properties
- **THEN** the provider SHALL record the event and forward to the configured backend (OpenTelemetry, Sentry, etc.)
- **AND** properties SHALL be key-value pairs (string keys, JSON-serializable values)

#### Scenario: TrackMetric records numeric metric
- **WHEN** C# or JS calls `TrackMetric(name, value, properties)` with metric name, value, and optional properties
- **THEN** the provider SHALL record the metric and forward to the configured backend

#### Scenario: TrackException records error
- **WHEN** C# or JS calls `TrackException(exception)` or `TrackException(message, stack)` with exception or message/stack
- **THEN** the provider SHALL record the exception for crash reporting
- **AND** Sentry provider SHALL capture it as an error event; OpenTelemetry SHALL record as exception span

### Requirement: Bridge call metrics auto-collection
When a telemetry provider is registered, the bridge SHALL automatically collect metrics for each bridge call.

#### Scenario: Export call metrics collected
- **WHEN** a JS→C# bridge call occurs and a telemetry provider is registered
- **THEN** the runtime SHALL record service name, method name, direction (export), latency (ms), and success/failure
- **AND** the metrics SHALL be forwarded to the provider (as span, metric, or breadcrumb per provider type)

#### Scenario: Import call metrics collected
- **WHEN** a C#→JS bridge call occurs and a telemetry provider is registered
- **THEN** the runtime SHALL record service name, method name, direction (import), latency (ms), and success/failure
- **AND** the metrics SHALL be forwarded to the provider

#### Scenario: Failed call includes error details
- **WHEN** a bridge call fails and a telemetry provider is registered
- **THEN** the runtime SHALL include error message (and optionally stack) in the reported metric/span
- **AND** TrackException MAY be called for unhandled bridge errors

### Requirement: OpenTelemetry built-in provider
A built-in provider SHALL integrate with OpenTelemetry for traces and metrics export.

#### Scenario: OpenTelemetry provider exports bridge spans
- **WHEN** the OpenTelemetry provider is configured and bridge calls occur
- **THEN** each call SHALL be recorded as an OpenTelemetry span (or metric)
- **AND** spans SHALL be exportable to OTLP or configured exporter

#### Scenario: OpenTelemetry provider is opt-in
- **WHEN** no OpenTelemetry provider is registered
- **THEN** no OpenTelemetry instrumentation SHALL be active
- **AND** bridge calls SHALL have no telemetry overhead

### Requirement: Sentry built-in provider
A built-in provider SHALL integrate with Sentry for crash reporting and error tracking.

#### Scenario: Sentry provider captures exceptions
- **WHEN** the Sentry provider is configured and TrackException is called (or unhandled exception occurs)
- **THEN** the exception SHALL be captured and sent to Sentry
- **AND** bridge call breadcrumbs MAY be attached for context

#### Scenario: Sentry provider is opt-in
- **WHEN** no Sentry provider is registered
- **THEN** no Sentry SDK SHALL be initialized
- **AND** no data SHALL be sent to Sentry

### Requirement: Bridge-integrated JS API (optional)
The bridge MAY expose a telemetry API to JS for custom events and errors.

#### Scenario: JS can report custom event
- **WHEN** the bridge exposes `telemetry.trackEvent(name, properties)` to JS
- **THEN** JS calls SHALL be forwarded to the registered C# provider
- **AND** the event SHALL be recorded with the same semantics as C# TrackEvent

#### Scenario: JS can report exception
- **WHEN** the bridge exposes `telemetry.trackException(message, stack)` to JS
- **THEN** JS calls SHALL be forwarded to the registered C# provider
- **AND** the exception SHALL be recorded for crash reporting (e.g., Sentry)
