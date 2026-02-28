## Why

Several adapter capabilities are no-op or missing on specific platforms, reducing the framework's cross-platform consistency. GTK ContextMenu is declared but never fires. GTK PrintToPdf is entirely missing. macOS DevTools runtime toggle is no-op without documentation. IAsyncPreloadScriptAdapter is Windows-only without clarity on other platforms. These gaps need to be addressed or explicitly documented.

**Goal alignment**: G1 (bridge/adapter completeness), E2 (cross-platform parity)
**ROADMAP**: Phase 8 M8.5 — Platform Feature Parity

## What Changes

- **GTK ContextMenu**: Wire WebKitGTK `context-menu` signal in native shim, raise `ContextMenuRequested` event in C# adapter
- **GTK PrintToPdf**: Document as platform limitation (WebKitGTK lacks PDF export API), update compatibility matrix
- **macOS DevTools**: Document `OpenDevTools()`/`CloseDevTools()` as no-op (WKWebView has no public API); note that inspector is available via right-click when `EnableDevTools` is set
- **IAsyncPreloadScriptAdapter**: Document as Windows-only (WKWebView, Android WebView, WebKitGTK lack async preload APIs), update compatibility matrix

## Non-goals

- Implementing PrintToPdf for GTK via Cairo surface (too complex, insufficient WebKitGTK API support)
- Using WKWebView private API `_WKInspector` for macOS DevTools toggle
- Implementing IAsyncPreloadScriptAdapter for non-Windows platforms

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `context-menu`: GTK adapter wires WebKitGTK `context-menu` signal, raising ContextMenuRequested with hit-test data
- `webview-compatibility-matrix`: Updated entries for GTK PrintToPdf, macOS DevTools, and IAsyncPreloadScriptAdapter cross-platform status

## Impact

- **Native C shim**: `WebKitGtkShim.c` — new callback typedef, struct field, and signal connection
- **GtkWebViewAdapter**: Wire context menu callback, raise event
- **NativeMethods (GTK)**: New P/Invoke delegate and struct field
- **Compatibility matrix doc**: Updated feature parity documentation
