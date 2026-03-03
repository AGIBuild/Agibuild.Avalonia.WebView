## Purpose

Define BDD-style requirements for the bridge call profiler: `BridgeCallProfiler`, statistics aggregation, query API, DevTools integration, and JSON export. Ensures developers can analyze bridge call performance without manual log inspection.

## Requirements

### Requirement: BridgeCallProfiler implements IBridgeTracer

`BridgeCallProfiler` SHALL implement `IBridgeTracer` and SHALL record call events for statistical aggregation.

#### Scenario: Export call start/end updates statistics
- **GIVEN** a `BridgeCallProfiler` instance
- **WHEN** `OnExportCallStart("AppService", "GetData", paramsJson)` is invoked
- **AND** `OnExportCallEnd("AppService", "GetData", 42, "object")` is invoked
- **THEN** the profiler SHALL record one successful export call for `AppService.GetData`
- **AND** the call count for that method SHALL be incremented by 1
- **AND** the latency (42 ms) SHALL be included in the method's latency statistics

#### Scenario: Export call error updates error count
- **GIVEN** a `BridgeCallProfiler` instance
- **WHEN** `OnExportCallStart("AppService", "GetData", null)` is invoked
- **AND** `OnExportCallError("AppService", "GetData", 15, exception)` is invoked
- **THEN** the profiler SHALL record one failed export call for `AppService.GetData`
- **AND** the error count for that method SHALL be incremented by 1
- **AND** the latency (15 ms) SHALL be included in the method's latency statistics

#### Scenario: Import call start/end updates statistics
- **GIVEN** a `BridgeCallProfiler` instance
- **WHEN** `OnImportCallStart("UiController", "ShowToast", paramsJson)` is invoked
- **AND** `OnImportCallEnd("UiController", "ShowToast", 8)` is invoked
- **THEN** the profiler SHALL record one successful import call for `UiController.ShowToast`
- **AND** the call count and latency SHALL be aggregated for that method

#### Scenario: Profiler delegates to inner tracer when provided
- **GIVEN** a `BridgeCallProfiler` constructed with an inner `IBridgeTracer`
- **WHEN** any `IBridgeTracer` callback is invoked on the profiler
- **THEN** the profiler SHALL record the event for statistics
- **AND** SHALL forward the callback to the inner tracer

### Requirement: Per-method statistics include call count, latency percentiles, error rate

Each method's statistics SHALL include: call count, min/avg/max latency, P50/P95/P99 latency, error count, and error rate.

#### Scenario: GetMethodStats returns aggregated statistics
- **GIVEN** a `BridgeCallProfiler` that has recorded 100 calls to `AppService.GetData` with latencies 1–100 ms (uniform)
- **WHEN** `GetMethodStats("AppService", "GetData")` is invoked
- **THEN** the result SHALL include `CallCount >= 100`
- **AND** SHALL include `MinLatencyMs`, `AvgLatencyMs`, `MaxLatencyMs`
- **AND** SHALL include `P50LatencyMs`, `P95LatencyMs`, `P99LatencyMs` (or equivalent percentile fields)
- **AND** SHALL include `ErrorCount` and `ErrorRate` (ErrorCount / CallCount when CallCount > 0)

#### Scenario: GetServiceStats aggregates all methods for a service
- **GIVEN** a `BridgeCallProfiler` that has recorded calls for `AppService.GetData` and `AppService.SaveData`
- **WHEN** `GetServiceStats("AppService")` is invoked
- **THEN** the result SHALL include aggregated or per-method statistics for all methods of that service
- **AND** SHALL allow identification of which methods contributed to the service totals

### Requirement: GetSlowestCalls returns top N calls by latency

The profiler SHALL provide `GetSlowestCalls(n)` (or equivalent) returning the N slowest calls, optionally filtered by service or method.

#### Scenario: GetSlowestCalls returns calls ordered by latency
- **GIVEN** a `BridgeCallProfiler` that has recorded calls with varying latencies
- **WHEN** `GetSlowestCalls(5)` is invoked
- **THEN** the result SHALL return at most 5 entries
- **AND** entries SHALL be ordered by latency descending (slowest first)
- **AND** each entry SHALL include service name, method name, latency, and call count (or equivalent)

### Requirement: Reset clears all statistics

The profiler SHALL support `Reset()` to clear all collected statistics.

#### Scenario: Reset clears profiler state
- **GIVEN** a `BridgeCallProfiler` with recorded statistics
- **WHEN** `Reset()` is invoked
- **THEN** `GetServiceStats()` for any service SHALL return empty or zeroed statistics
- **AND** `GetMethodStats()` for any method SHALL return empty or zeroed statistics
- **AND** `GetSlowestCalls(n)` SHALL return no entries (or empty list)

### Requirement: Export as JSON for external tools

The profiler SHALL support exporting its state as JSON for consumption by external tools (e.g., VS Code extension).

#### Scenario: ExportToJson produces valid JSON
- **GIVEN** a `BridgeCallProfiler` with recorded statistics
- **WHEN** `ExportToJson()` (or equivalent) is invoked
- **THEN** the result SHALL be valid JSON
- **AND** SHALL include service names, method names, and their statistics
- **AND** SHALL be parseable by standard JSON libraries

### Requirement: Integration with BridgeDevToolsService

When the profiler is composed with `BridgeDevToolsService`, the overlay SHALL be able to display profiler statistics.

#### Scenario: DevTools overlay can display profiler data
- **GIVEN** a `BridgeDevToolsService` configured with a `BridgeCallProfiler` (e.g., via `CompositeBridgeTracer` or constructor)
- **WHEN** the DevTools overlay is shown and requests profiler data
- **THEN** the overlay SHALL receive statistics via the profiler's query API or a dedicated channel
- **AND** SHALL display at least service-level or method-level stats (e.g., slowest calls, error rates)

### Requirement: CompositeBridgeTracer composes multiple tracers

A `CompositeBridgeTracer` SHALL forward all `IBridgeTracer` callbacks to each composed tracer.

#### Scenario: CompositeBridgeTracer forwards to all tracers
- **GIVEN** a `CompositeBridgeTracer` containing a `BridgeCallProfiler` and a `DevToolsPanelTracer`
- **WHEN** `OnExportCallStart("Svc", "M", null)` is invoked on the composite
- **THEN** the profiler SHALL receive the callback
- **AND** the DevTools tracer SHALL receive the callback
- **AND** both SHALL process the event independently
