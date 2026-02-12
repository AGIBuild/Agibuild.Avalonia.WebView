# DevTools Toggle Spec

## Overview
Runtime API to open/close browser developer tools.

## Requirements

### DT-1: IDevToolsAdapter
- Internal interface in Abstractions
- OpenDevTools(), CloseDevTools(), IsDevToolsOpen

### DT-2: IWebView surface
- Public OpenDevTools(), CloseDevTools(), IsDevToolsOpen on IWebView
- No-op when adapter does not implement IDevToolsAdapter

### DT-3: Platform support
- Windows (WebView2): OpenDevTools via OpenDevToolsWindow()
- macOS/iOS: no-op (no public WKWebView API)
- Android: no-op (debug controlled at init)
- GTK: no-op (TODO: webkit_web_inspector_show)

## Test Coverage
- 3 CTs in DevToolsTests
