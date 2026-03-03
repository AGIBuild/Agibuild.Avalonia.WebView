# Bridge Call Profiler — Tasks

## 1. Profiler Core

- [x] 1.1 Create `BridgeCallProfiler` class implementing `IBridgeTracer`

- [x] 1.2 Add constructor accepting optional `IBridgeTracer? inner` for delegation

- [x] 1.3 Implement `OnExportCallStart` / `OnExportCallEnd` / `OnExportCallError` — record events and delegate to inner

- [x] 1.4 Implement `OnImportCallStart` / `OnImportCallEnd` — record events and delegate to inner

- [x] 1.5 Implement `OnServiceExposed` / `OnServiceRemoved` — delegate to inner (no stats aggregation for lifecycle)

- [x] 1.6 Create `CompositeBridgeTracer` that holds `IReadOnlyList<IBridgeTracer>` and forwards each callback to all

## 2. Statistics Aggregation

- [x] 2.1 Define `MethodStats` (or equivalent) type with: CallCount, ErrorCount, MinLatencyMs, AvgLatencyMs, MaxLatencyMs, P50LatencyMs, P95LatencyMs, P99LatencyMs, ErrorRate

- [x] 2.2 Use `ConcurrentDictionary<string, MethodStats>` (or per-method structure) for thread-safe storage keyed by service+method

- [x] 2.3 Implement latency histogram (HdrHistogram-style or lightweight buckets) for percentile computation

- [x] 2.4 On each `OnExportCallEnd` / `OnImportCallEnd` / `OnExportCallError`, update the corresponding method's stats (call count, error count, latency)

- [x] 2.5 Implement `Reset()` to clear all statistics

## 3. API Surface

- [x] 3.1 Implement `GetMethodStats(string serviceName, string methodName)` returning `MethodStats?` or equivalent

- [x] 3.2 Implement `GetServiceStats(string serviceName)` returning aggregated stats for all methods of a service

- [x] 3.3 Implement `GetSlowestCalls(int n)` returning top N methods by latency (e.g., by max or P95)

- [x] 3.4 Implement `GetAllStats()` returning all service/method statistics (for DevTools overlay)

- [x] 3.5 Add XML documentation for public API

## 4. DevTools Integration

- [x] 4.1 Extend `BridgeDevToolsService` constructor or overload to accept optional `BridgeCallProfiler` (or compose via `CompositeBridgeTracer`)

- [x] 4.2 Add profiler data channel to the overlay: push stats or expose via a script-invokable endpoint

- [x] 4.3 Update DevTools overlay HTML/JS to display a "Profiler" tab or section showing `GetServiceStats()` / `GetSlowestCalls(n)`

- [x] 4.4 Ensure profiler can be composed with existing DevTools tracer: `CompositeBridgeTracer(profiler, devToolsService.Tracer)`

## 5. JSON Export

- [x] 5.1 Implement `ExportToJson()` (or `ToJson()`) returning a JSON string with all profiler state

- [x] 5.2 Include service names, method names, and their statistics in the JSON structure

- [x] 5.3 Use `System.Text.Json` for serialization (consistent with existing Fulora usage)

- [x] 5.4 Document JSON schema for VS Code extension consumption

## 6. Configuration & DI

- [x] 6.1 Add `EnableProfiler` to the appropriate options type (e.g., `WebMessageBridgeOptions`, `FuloraServiceBuilder`, or a dedicated profiler options)

- [x] 6.2 When `EnableProfiler` is true, register `BridgeCallProfiler` and compose with the active tracer

- [x] 6.3 Add DI extension method if applicable (e.g., `AddBridgeProfiler()`)

## 7. Tests

- [x] 7.1 Create `BridgeCallProfilerTests` in `tests/Agibuild.Fulora.UnitTests/`

- [x] 7.2 Test `OnExportCallStart`/`End`/`Error` updates statistics correctly

- [x] 7.3 Test `OnImportCallStart`/`End` updates statistics correctly

- [x] 7.4 Test `GetMethodStats` returns correct values after multiple calls

- [x] 7.5 Test `GetServiceStats` aggregates per-method stats

- [x] 7.6 Test `GetSlowestCalls(n)` returns correct ordering

- [x] 7.7 Test `Reset()` clears all statistics

- [x] 7.8 Test `ExportToJson()` produces valid JSON

- [x] 7.9 Test `CompositeBridgeTracer` forwards to all composed tracers

- [x] 7.10 Test profiler delegates to inner tracer when provided
