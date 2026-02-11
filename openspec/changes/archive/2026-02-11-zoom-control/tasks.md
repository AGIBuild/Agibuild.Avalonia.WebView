# Tasks — zoom-control

## 1. Core Contracts
- [x] Add `IZoomAdapter` facet interface to `IWebViewAdapter.cs`

## 2. Runtime Integration
- [x] Add `IZoomAdapter` facet detection in `WebViewCore` constructor
- [x] Add `ZoomFactor` property (get/set, clamped 0.25–5.0) to `WebViewCore`
- [x] Add `ZoomFactorChanged` event to `WebViewCore`
- [x] Expose `ZoomFactor` / `ZoomFactorChanged` on `WebDialog`
- [x] Add `ZoomFactor` as `StyledProperty<double>` on `WebView`
- [x] Wire `WebView.ZoomFactor` property change to `WebViewCore`

## 3. Platform Adapters — macOS
- [x] Add native `ag_wk_get_zoom` / `ag_wk_set_zoom` to `WkWebViewShim.mm`
- [x] Add P/Invoke declarations in `MacOSWebViewAdapter.PInvoke.cs`
- [x] Implement `IZoomAdapter` in macOS adapter

## 4. Platform Adapters — Windows
- [x] Implement `IZoomAdapter` in `WindowsWebViewAdapter.cs` via `CoreWebView2Controller.ZoomFactor`

## 5. Platform Adapters — iOS
- [x] Add native zoom functions to `WkWebViewShim.iOS.mm`
- [x] Implement `IZoomAdapter` in `iOSWebViewAdapter.cs`

## 6. Platform Adapters — Android
- [x] Implement `IZoomAdapter` in `AndroidWebViewAdapter.cs` via `WebSettings.setTextZoom`

## 7. Platform Adapters — GTK
- [x] Add native `ag_gtk_get_zoom` / `ag_gtk_set_zoom` to `WebKitGtkShim.c`
- [x] Implement `IZoomAdapter` in `GtkWebViewAdapter.cs`

## 8. Testing
- [x] Add `MockWebViewAdapterWithZoom` to `MockWebViewAdapter.cs`
- [x] Add contract tests: default 1.0, set/get, clamping, event, no-op without adapter
- [x] Add integration tests (headless): zoom set/get, event via mock adapter
- [x] Verify all tests pass
- [x] Verify coverage >= 90%
