# Design: Bridge Tracing & DevTools

## Architecture

### IBridgeTracer (Core)
Interface with 7 methods:
- `OnExportCallStart(serviceName, methodName, paramsJson?)` 
- `OnExportCallEnd(serviceName, methodName, elapsedMs, resultType?)`
- `OnExportCallError(serviceName, methodName, elapsedMs, error)`
- `OnImportCallStart(serviceName, methodName, paramsJson?)`
- `OnImportCallEnd(serviceName, methodName, elapsedMs)`
- `OnServiceExposed(serviceName, methodCount, isSourceGenerated)`
- `OnServiceRemoved(serviceName)`

### LoggingBridgeTracer (Runtime)
- Delegates to ILogger with LogTrace/LogWarning/LogInformation
- Truncates params to 200 chars for safety
- Structured log templates for ELK/Seq/OTLP consumption

### NullBridgeTracer (Core)
- Singleton, all methods empty — compiled away by JIT

### RuntimeBridgeService integration
- Constructor accepts optional `IBridgeTracer?` (defaults to NullBridgeTracer.Instance)
- Fires OnServiceExposed in Expose (both SG and reflection paths)
- Fires OnServiceRemoved in Remove

### IDevToolsAdapter (Abstractions)
Internal optional interface: OpenDevTools(), CloseDevTools(), IsDevToolsOpen
- Windows: `CoreWebView2.OpenDevToolsWindow()` (Close not available in WebView2 API)
- macOS: no public WKWebView API (isInspectable set at init)
- GTK: WebKitGTK supports `webkit_web_inspector_show()` (TODO when production-ready)
- Android/iOS: no runtime toggle

### IWebView (Core)
Public surface: OpenDevTools(), CloseDevTools(), IsDevToolsOpen
- WebViewCore: pattern matches adapter as IDevToolsAdapter
- WebDialog/WebView/AvaloniaWebDialog: delegate
- TestWebViewHost: no-op stubs

## Testing
- 5 tracer tests (NullBridgeTracer, LoggingBridgeTracer, custom recorder)
- 3 DevTools tests (interface shape, WebViewCore no-throw, TestWebViewHost)
