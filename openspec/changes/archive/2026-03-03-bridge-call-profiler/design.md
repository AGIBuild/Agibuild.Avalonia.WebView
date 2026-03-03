## Context

Fulora's `IBridgeTracer` emits raw call events (OnExportCallStart/End/Error, OnImportCallStart/End, OnServiceExposed/Removed). `BridgeEventCollector` and `BridgeDevToolsService` provide in-app visualization of individual calls. The VS Code extension (separate change) will consume profiler data for IDE-side visualization. This change adds a bridge call profiler that aggregates performance statistics so developers can answer "which calls are slowest?" and "what is P95 latency for this service?" without manual log analysis.

**Existing contracts**: `IBridgeTracer`, `BridgeEventCollector`, `BridgeDevToolsService`, `DevToolsPanelTracer` (inner-tracer delegation pattern), `BridgeTelemetryTracer` (same pattern).

## Goals / Non-Goals

**Goals:**
- `BridgeCallProfiler : IBridgeTracer` that collects per-service, per-method statistics
- Statistics: call count, min/avg/max/P50/P95/P99 latency, error rate
- Configurable time window (sliding window or since-reset)
- Query API: `GetServiceStats()`, `GetMethodStats()`, `GetSlowestCalls(n)`
- Composable with other tracers (DevTools, logging, telemetry)
- Integration with `BridgeDevToolsService` for overlay visualization
- JSON export for external tools (e.g., VS Code extension)

**Non-Goals:**
- Memory profiling, GC profiling
- Automatic performance recommendations
- Production profiling (dev-time only)

## Decisions

### D1: BridgeCallProfiler implements IBridgeTracer

**Choice**: `BridgeCallProfiler` implements `IBridgeTracer` and receives all bridge call events. It computes statistics internally and optionally delegates to an inner tracer.

**Rationale**: Same pattern as `DevToolsPanelTracer` and `BridgeTelemetryTracer`. Pluggable into the existing tracer pipeline without changing `RuntimeBridgeService`.

### D2: Lock-free ConcurrentDictionary for stats storage

**Choice**: Use `ConcurrentDictionary<string, MethodStats>` keyed by `"{serviceName}.{methodName}"` (or similar) for per-method statistics. Each `MethodStats` uses lock-free or minimal-lock updates (e.g., `Interlocked` for counters, per-key locking for histogram updates).

**Rationale**: Bridge calls can occur from multiple threads (export from JS thread, import from C# thread). ConcurrentDictionary avoids global lock contention. Per-method granularity matches developer mental model.

### D3: HdrHistogram-style latency tracking

**Choice**: Use an HdrHistogram-inspired approach for latency percentiles: fixed-size buckets (e.g., 1ms, 2ms, 4ms, … up to a max) or a lightweight histogram implementation. Compute P50/P95/P99 from the histogram on query.

**Rationale**: O(1) record, bounded memory. HdrHistogram is industry-standard for latency; a simplified in-process implementation avoids external dependency. Alternatively, use `HdrHistogram` NuGet package if acceptable.

### D4: Composable with other tracers via CompositeBridgeTracer

**Choice**: Introduce `CompositeBridgeTracer` that holds multiple `IBridgeTracer` instances and forwards each callback to all. `BridgeCallProfiler` can be one of the composed tracers. Alternatively, use the existing inner-tracer pattern: `BridgeCallProfiler(DevToolsPanelTracer(collector, LoggingBridgeTracer(...)))`.

**Rationale**: Developers often want profiler + DevTools + logging. A `CompositeBridgeTracer` simplifies composition: `new CompositeBridgeTracer(profiler, devToolsTracer, loggingTracer)`. Avoids deep nesting of inner tracers.

### D5: Time window: since-reset by default, optional sliding window

**Choice**: Default behavior: statistics accumulate since last `Reset()`. Optional: configurable sliding time window (e.g., last 5 minutes) where older samples are dropped on each update or on query.

**Rationale**: Since-reset is simplest and matches "start session, inspect stats" workflow. Sliding window is useful for long-running dev sessions; can be added in a follow-up if needed.

### D6: BridgeDevToolsService integration

**Choice**: `BridgeDevToolsService` accepts an optional profiler. When provided, the overlay can display a "Profiler" tab or section showing `GetServiceStats()` / `GetSlowestCalls(n)`. The profiler is composed as a tracer (e.g., `BridgeDevToolsService(profiler)` so profiler receives events; or `BridgeDevToolsService(CompositeBridgeTracer(profiler, devToolsTracer))`).

**Rationale**: In-app visualization without requiring external tools. VS Code extension consumes JSON export; DevTools overlay provides quick in-app view.

### D7: EnableProfiler configuration

**Choice**: Add `EnableProfiler` to the appropriate configuration surface (e.g., `WebMessageBridgeOptions`, `FuloraServiceBuilder`, or a dedicated profiler options type). When enabled, `BridgeCallProfiler` is registered and composed with the active tracer.

**Rationale**: Profiler has overhead (histogram updates, memory). Opt-in for dev-time use. Exact placement depends on existing DI/builder patterns.

## Risks / Trade-offs

- **[Risk] Histogram memory** → Per-method histograms can grow with many services/methods. Cap bucket count or use a fixed-size implementation.
- **[Trade-off] HdrHistogram dependency** → Use built-in lightweight histogram vs. `HdrHistogram` package. Built-in keeps dependencies minimal; package gives proven percentile accuracy.
- **[Trade-off] Sliding window complexity** → Defer to Phase 2 if since-reset suffices for initial release.
