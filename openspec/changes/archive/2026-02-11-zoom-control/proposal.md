## Why

Zoom/scale control is a fundamental browser capability. Users expect Ctrl+/- and pinch-to-zoom in embedded browsers. Electron exposes `webContents.setZoomFactor()` / `getZoomFactor()` / `setZoomLevel()`. Our WebView has no zoom API, preventing consumers from implementing accessibility zoom, presentation mode, or user-preference scaling.

## What Changes

- Add `IZoomAdapter` facet interface for platform adapters
- Add `ZoomFactor` property (get/set, double, 1.0 = 100%) to `WebViewCore` / `WebDialog` / `WebView`
- Add `ZoomFactorChanged` event
- Implement native zoom on all 5 platform adapters:
  - Windows: `CoreWebView2Controller.ZoomFactor`
  - macOS: `WKWebView.pageZoom` or `magnification`
  - iOS: `WKWebView.scrollView.zoomScale` (read) + viewport meta (set)
  - Android: `WebSettings.setTextZoom()` or `WebView.zoomIn()/zoomOut()`
  - GTK: `webkit_web_view_set_zoom_level()`

## Capabilities

### New Capabilities
- `zoom-control`: Programmatic zoom factor get/set with change notification

### Modified Capabilities
- `webview-compatibility-matrix`: Add zoom-control as Extended capability entry

## Impact

- New facet interface in `IWebViewAdapter.cs`
- New `ZoomFactor` property + event on `WebView`, `WebViewCore`, `WebDialog`
- Native shim additions for macOS/iOS, GTK
- Platform adapter implementations (5 adapters)
- New unit + integration tests
