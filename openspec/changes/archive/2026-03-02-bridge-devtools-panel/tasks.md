## 1. Tracer-Backed Event Collector

- [x] 1.1 Define `IBridgeEventCollector` interface for bounded event buffer with add/read/clear semantics
- [x] 1.2 Implement `DevToolsPanelTracer` that implements `IBridgeTracer` and forwards events to a bounded ring buffer
- [x] 1.3 Add configurable buffer capacity (500 entries default) and overflow handling (drop oldest, expose dropped count)
- [x] 1.4 Add contract tests: verify collector receives all tracer callbacks and buffers correctly under load

## 2. Overlay Data Model and Binding

- [x] 2.1 Define event DTOs for overlay consumption (service, method, direction, params, result/error, elapsedMs, timestamp)
- [x] 2.2 Implement subscriber pattern for overlay to subscribe to collector events
- [x] 2.3 Add payload truncation for large params/result (max 4KB per field) to prevent memory bloat

## 3. Overlay Web UI

- [x] 3.1 Create overlay HTML/CSS/JS assets: list view of bridge calls with expandable rows
- [x] 3.2 Implement real-time update of call list (via invokeScript from host)
- [x] 3.3 Display direction (JS→C# / C#→JS), service, method, latency, and expandable params/result/error
- [x] 3.4 Style error entries distinctly (red background)
- [x] 3.5 Add clear/reset button to flush displayed logs

## 4. Host Integration

- [x] 4.1 Create `BridgeDevToolsService` as the main entry point for enabling the panel
- [x] 4.2 Create overlay container component (Avalonia overlay layer) that hosts the overlay WebView
- [x] 4.3 Wire collector tracer into `RuntimeBridgeService` when panel is enabled
- [x] 4.4 Add toggle mechanism (keyboard shortcut) to show/hide the overlay
- [x] 4.5 Integrate overlay into `dotnet new agibuild-hybrid` template (optional panel region)

## 5. Documentation and Tests

- [x] 5.1 Add unit tests: collector buffer, tracer delegation, truncation, service integration
- [x] 5.2 Add integration test: expose service → trigger bridge call → verify overlay receives entry
- [x] 5.3 Document Bridge DevTools Panel: how to enable, what it shows
