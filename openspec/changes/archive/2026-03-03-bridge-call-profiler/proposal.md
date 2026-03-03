## Why

Fulora's `IBridgeTracer` provides raw call events but no aggregated performance analysis. Developers cannot answer "which bridge calls are slowest?" or "what's the P95 latency for this service?" without manual log analysis. A profiler component that implements `IBridgeTracer` and computes statistics would enable performance debugging. Goal: Phase 11, E2 (Dev Tooling).

## What Changes

- New class `BridgeCallProfiler : IBridgeTracer` that collects call statistics
- Per-service, per-method stats: call count, min/avg/max/P50/P95/P99 latency, error rate
- Configurable time window (sliding window or since-reset)
- API to query profiler state: `GetServiceStats()`, `GetMethodStats()`, `GetSlowestCalls(n)`
- Integration with `BridgeDevToolsService` for visualization
- Export as JSON for external tools

## Capabilities

### New Capabilities

- `bridge-call-profiler`: Bridge call performance profiler with statistical aggregation

### Modified Capabilities

- `bridge-tracing`: Add profiler as composable tracer alongside existing tracers

## Non-goals

- Memory profiling, GC profiling
- Automatic performance recommendations
- Continuous production profiling (this is dev-time)

## Impact

- New: `src/Agibuild.Fulora.Runtime/BridgeCallProfiler.cs`
- Modified: BridgeOptions (EnableProfiler), DI registration
- New tests
