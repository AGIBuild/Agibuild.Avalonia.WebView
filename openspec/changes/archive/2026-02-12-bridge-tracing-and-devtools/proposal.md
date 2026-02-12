# Bridge Tracing & DevTools Toggle

## Problem
- No structured observability for bridge RPC calls (latency, errors, throughput)
- No runtime API to toggle DevTools — only init-time configuration via EnableDevTools

## Solution
### 3.2: IBridgeTracer
- Pluggable `IBridgeTracer` interface in Core with 7 event methods (export start/end/error, import start/end, service exposed/removed)
- `LoggingBridgeTracer` (Runtime) — default implementation using ILogger with structured log templates
- `NullBridgeTracer` — zero-overhead no-op for production

### 3.3: IDevToolsAdapter + IWebView API
- `IDevToolsAdapter` internal interface (Abstractions): OpenDevTools(), CloseDevTools(), IsDevToolsOpen
- `IWebView.OpenDevTools()`, `CloseDevTools()`, `IsDevToolsOpen` — public API
- WebViewCore delegates to adapter via pattern matching; no-op if adapter doesn't support it
- Windows: WebView2 `OpenDevToolsWindow()` for Open
- macOS/iOS/Android/GTK: no-op placeholders (platform limitations)

## Non-goals
- Bridge method call visualization overlay (deferred to future)
- Per-method tracing filters

## References
G1 (Type-Safe Bridge), E2 (Developer Experience), ROADMAP 3.2, 3.3
