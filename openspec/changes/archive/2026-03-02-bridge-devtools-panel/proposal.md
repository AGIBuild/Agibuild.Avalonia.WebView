## Why

Developers debugging C# ↔ JS bridge communication today rely on `IBridgeTracer` (structured logging) or platform DevTools. Neither provides an in-app, real-time view of bridge calls with request/response payloads, latency, and error details. This gap slows debugging and reduces the value of the existing tracing infrastructure.

**Goal alignment**: E2 (Dev Tooling), Phase 3 / 3.2 (Bridge call tracing + logging). Extends the tracer into a visible, interactive debug experience.

## What Changes

- Add a **Bridge DevTools Panel** — an in-app, web-based debug overlay that displays real-time bridge call logs, request/response payloads, latency metrics, and error details
- Introduce a tracer-backed event collector that buffers bridge events for the panel
- Provide a toggle to show/hide the panel (opt-in, development-only by default)
- Host the panel UI as a WebView overlay or embedded HTML view, fed by bridge tracer events

## Non-goals

- Replacing platform DevTools (Chrome/WKWebView inspector) — they remain for DOM/network debugging
- Production deployment of the panel — it is a development aid, gated by policy or build configuration
- Full performance profiler — focus is on call visibility and payload inspection, not flame graphs

## Capabilities

### New Capabilities
- `bridge-debug-overlay`: In-app debug overlay showing real-time bridge call logs, payloads, latency, and errors, driven by `IBridgeTracer` events

### Modified Capabilities
- None (uses existing `bridge-tracing` and `devtools-toggle` contracts; no spec changes required)

## Impact

- New runtime component: `BridgeDevToolsPanel` or equivalent service that collects tracer events and serves them to the overlay UI
- New web assets: HTML/CSS/JS for the overlay UI (embedded or served via `app://`)
- Integration point: Shell or WebView host to mount the overlay and wire tracer → panel
- Policy: DevTools policy or environment flag to enable/disable the panel in production builds
