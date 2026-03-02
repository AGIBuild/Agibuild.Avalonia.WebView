## Purpose

Define the Bridge DevTools Panel — an in-app debug overlay that displays real-time bridge call logs, request/response payloads, latency metrics, and error details for C# ↔ JS communication debugging.

## Requirements

### Requirement: Tracer-backed event collector is provided
Runtime SHALL provide an `IBridgeTracer` implementation that buffers bridge events (export/import start/end/error, service exposed/removed) for consumption by the debug overlay.

#### Scenario: Collector receives export call events
- **WHEN** a JS→C# method invocation occurs and the collector tracer is active
- **THEN** the collector SHALL buffer `OnExportCallStart`, `OnExportCallEnd` or `OnExportCallError` with service name, method name, params, result/error, and elapsed time

#### Scenario: Collector receives import call events
- **WHEN** a C#→JS method invocation occurs and the collector tracer is active
- **THEN** the collector SHALL buffer `OnImportCallStart` and `OnImportCallEnd` with service name, method name, params, and elapsed time

#### Scenario: Collector uses bounded buffer
- **WHEN** the event buffer reaches its configured capacity
- **THEN** the collector SHALL drop the oldest entries and SHALL indicate overflow to the overlay (e.g. via a dropped-count or overflow flag)

### Requirement: Debug overlay displays real-time bridge call logs
The overlay UI SHALL display a chronological list of bridge calls with direction (export/import), service name, method name, and timestamp.

#### Scenario: Export call appears in overlay
- **WHEN** a JS→C# call completes and the overlay is visible
- **THEN** the overlay SHALL show an entry with direction "JS→C#", service name, method name, and timestamp

#### Scenario: Import call appears in overlay
- **WHEN** a C#→JS call completes and the overlay is visible
- **THEN** the overlay SHALL show an entry with direction "C#→JS", service name, method name, and timestamp

### Requirement: Overlay shows request and response payloads
The overlay SHALL allow inspection of request (params) and response (result or error) payloads for each bridge call.

#### Scenario: Params are visible for a call
- **WHEN** a user expands or selects a bridge call entry in the overlay
- **THEN** the overlay SHALL display the request params (JSON or equivalent) for that call

#### Scenario: Result or error is visible for a call
- **WHEN** a user expands or selects a completed bridge call entry
- **THEN** the overlay SHALL display the result payload (on success) or error message and details (on failure)

### Requirement: Overlay shows latency metrics
The overlay SHALL display elapsed time (latency) for each bridge call.

#### Scenario: Latency is shown per call
- **WHEN** a bridge call completes
- **THEN** the overlay SHALL display the elapsed time (e.g. in milliseconds) for that call

### Requirement: Overlay shows error details for failed calls
The overlay SHALL prominently display error information when a bridge call fails.

#### Scenario: Export call error is visible
- **WHEN** a JS→C# call fails and the overlay is visible
- **THEN** the overlay SHALL show the error message and SHALL distinguish failed calls (e.g. via styling or icon)

### Requirement: Panel is opt-in and development-oriented
The Bridge DevTools Panel SHALL be disabled by default and SHALL be gated by an explicit enablement mechanism (environment option or policy).

#### Scenario: Panel is off when not enabled
- **WHEN** the panel is not explicitly enabled
- **THEN** the overlay SHALL NOT be shown and the collector tracer SHALL NOT be attached (or SHALL be a no-op)

#### Scenario: Panel can be toggled when enabled
- **WHEN** the panel is enabled
- **THEN** the host SHALL provide a way to show or hide the overlay (e.g. keyboard shortcut or button)
