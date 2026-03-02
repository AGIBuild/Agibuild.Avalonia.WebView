## Context

Agibuild.Fulora has a type-safe bidirectional bridge (`[JsExport]` / `[JsImport]`) with JSON-RPC 2.0. The bridge already has:
- `IBridgeTracer` in Core — export/import call lifecycle and service expose/remove events
- `LoggingBridgeTracer` and `NullBridgeTracer` in Runtime
- `RuntimeBridgeService` accepts an optional tracer and emits hooks on all bridge operations
- DevTools toggle API (`OpenDevToolsAsync`, `CloseDevToolsAsync`, `IsDevToolsOpenAsync`) for platform inspector

**Gap**: No in-app UI to visualize bridge traffic. Developers must parse logs or use platform DevTools (which do not surface bridge RPC semantics). Phase 3 / 3.2 delivered tracing; this change delivers a visible debug overlay.

**Phase alignment**: E2 (Dev Tooling), Phase 3 / 3.2 (Bridge call tracing + logging).

## Goals / Non-Goals

**Goals:**
- In-app debug overlay showing real-time bridge call logs
- Request/response payloads, latency metrics, error details
- Opt-in, development-oriented; no production impact when disabled
- Reuse `IBridgeTracer` as the data source

**Non-Goals:**
- Replacing platform DevTools
- Production deployment of the panel
- Full performance profiler (flame graphs, heap snapshots)

## Decisions

### D1: Data source — tracer-backed collector

**Choice**: Implement an `IBridgeTracer` that buffers events in a bounded ring buffer and exposes them to the overlay via a channel or observable.

**Alternatives considered**:
- Instrument `RuntimeBridgeService` directly: Couples overlay to runtime internals; violates tracer abstraction
- Separate telemetry pipeline: Overkill; tracer already has the right hooks

**Rationale**: Tracer is the canonical observability hook. A `DevToolsPanelTracer` (or similar) implements `IBridgeTracer`, wraps the app's tracer, and feeds a bounded buffer. The overlay subscribes to that buffer.

### D2: Overlay UI technology

**Choice**: Web-based UI (HTML/CSS/JS) served via `app://` or embedded as a WebView overlay.

**Alternatives considered**:
- Native Avalonia controls: More work; web UI matches bridge debugging context (C# ↔ JS)
- Separate window: Adds complexity; overlay is simpler and keeps focus on the app

**Rationale**: Web UI is consistent with the bridge domain and allows rapid iteration. Can reuse existing SPA hosting or a minimal static page.

### D3: Overlay mounting

**Choice**: Overlay is a floating panel (or docked region) that the host app mounts when the panel is enabled. Host provides a container (e.g. `ContentControl` or overlay layer) and wires the tracer → panel data path.

**Alternatives considered**:
- Automatic injection into every WebView: Intrusive; may conflict with app layout
- Standalone DevTools window: More complex; overlay is sufficient for call inspection

**Rationale**: Explicit mounting gives apps control over placement and lifecycle. Template can provide a default "DevTools panel" region.

### D4: Enablement policy

**Choice**: Panel is enabled via environment option (e.g. `EnableBridgeDevTools`) or shell policy, defaulting to off in release builds.

**Alternatives considered**:
- Always on when Debug: Simpler but may leak in some build configs
- Runtime flag only: Less discoverable; env + policy is clearer

**Rationale**: Aligns with `devtools-toggle` and `EnableDevTools` patterns. Production builds should not ship the panel.

## Risks / Trade-offs

- **[Risk] Buffer overflow under heavy traffic** → Use bounded ring buffer; drop oldest entries with indicator. Document limit.
- **[Risk] Payload size / PII in logs** → Truncate large payloads; consider redaction for sensitive fields (future).
- **[Trade-off] Overlay adds complexity to host setup** → Mitigate with template integration and clear docs.
- **[Trade-off] WebView overlay may have platform quirks** → Reuse existing WebView contracts; test on Windows/macOS/Linux.

## Testing Strategy

- **Contract tests**: Mock `IBridgeTracer`; verify collector receives and buffers events correctly
- **Unit tests**: Panel data model (event serialization, truncation, ring buffer behavior)
- **Integration tests**: End-to-end: expose service → trigger bridge call → verify overlay shows entry
- **Governance**: Panel does not affect bridge semantics when disabled; `NullBridgeTracer` path unchanged

## Migration Plan

1. Add `DevToolsPanelTracer` (or equivalent) implementing `IBridgeTracer`
2. Add overlay web assets and data binding (e.g. WebSocket or postMessage from host)
3. Add host integration point (overlay container + enablement)
4. Document in Getting Started / bridge guide

## Open Questions

- Should the overlay support filtering by service/method?
- Should we support export/import of logs for offline analysis?
