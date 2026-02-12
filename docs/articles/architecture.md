# Architecture

## Layer Diagram

```
┌─────────────────────────────────────────────────┐
│  Consumer App (Avalonia XAML + Code-behind)      │
│  ┌──────────┐  ┌────────────┐  ┌─────────────┐  │
│  │ WebView  │  │ WebDialog  │  │ Ava.Dialog  │  │
│  └────┬─────┘  └─────┬──────┘  └──────┬──────┘  │
│       │               │                │         │
│  ┌────┴───────────────┴────────────────┘         │
│  │            WebViewCore                        │
│  │  ┌─────────────┐  ┌──────────────────┐        │
│  │  │ Bridge      │  │ SPA Hosting      │        │
│  │  │ (IBridge)   │  │ (SpaHostingSvc)  │        │
│  │  └──────┬──────┘  └────────┬─────────┘        │
│  │         │                  │                  │
│  │  ┌──────┴──────┐  ┌───────┴──────────┐        │
│  │  │ RPC Service │  │ WebResource      │        │
│  │  │ (JSON-RPC)  │  │ Interception     │        │
│  │  └──────┬──────┘  └────────┬─────────┘        │
│  └─────────┼──────────────────┼──────────────────┘
│            │                  │                   │
│  ┌─────────┴──────────────────┴──────────────────┐
│  │          IWebViewAdapter (Abstraction)         │
│  └────────────────────┬──────────────────────────┘
│                       │                           │
│  ┌────────┬───────────┼───────────┬──────────────┐
│  │Windows │  macOS    │  Android  │  GTK/Linux   │
│  │WebView2│  WKWebView│  WebView  │  WebKitGTK   │
│  └────────┴───────────┴───────────┴──────────────┘
└─────────────────────────────────────────────────┘
```

## Key Design Principles

### 1. Contract-First
Every feature starts as an interface in Core. Implementation is in Runtime. Platform specifics are in Adapters.

### 2. Facet-Based Adapters
Platform adapters implement optional interfaces (facets):
- `IWebViewAdapter` (required) — lifecycle, navigation
- `ICookieAdapter` — cookie management
- `IZoomAdapter` — zoom control
- `IFindInPageAdapter` — find in page
- `IDevToolsAdapter` — runtime DevTools toggle
- etc.

WebViewCore uses pattern matching (`adapter as IFoo`) to check capabilities.

### 3. AOT-Safe by Design
- Roslyn Source Generator eliminates reflection for bridge dispatch
- `System.Text.Json` with source-generated contexts
- No `dynamic`, no `Activator.CreateInstance` in hot paths

### 4. Testable Without a Browser
- `MockWebViewAdapter` simulates all adapter behavior
- `TestDispatcher` controls async execution timing
- `MockBridgeService` for consumer unit tests

## Bridge Architecture

```
C# Service                    JavaScript
  │                               │
  │  [JsExport]                   │
  ├──→ RuntimeBridgeService       │
  │    ├─ SG path (AOT)           │
  │    └─ Reflection fallback     │
  │         │                     │
  │    WebViewRpcService          │
  │    (JSON-RPC 2.0)             │
  │         │                     │
  │    WebMessage ◄──────────────►│ window.agWebView.rpc
  │                               │
  │  [JsImport]                   │
  │    GetProxy<T>()              │
  │    ├─ SG proxy (AOT)          │
  │    └─ DispatchProxy fallback  │
  └──→ JSON-RPC invoke ──────────►│
```

## Security Layers

1. **WebMessage Policy** — origin + channel + protocol filtering
2. **Rate Limiting** — per-service sliding-window (`BridgeOptions.RateLimit`)
3. **Explicit Exposure** — only `[JsExport]`-decorated interfaces are accessible
