# Screenshot Capture — Tasks

## Core Contracts
- [x] Add `CaptureScreenshotAsync()` to `IWebView` interface

## Adapter Abstractions
- [x] Add `IScreenshotAdapter` facet interface

## Runtime
- [x] Update `WebViewCore` to detect `IScreenshotAdapter` and store reference
- [x] Implement `WebViewCore.CaptureScreenshotAsync()` delegating to adapter or throwing `NotSupportedException`

## Platform Adapters — Windows
- [x] Implement `IScreenshotAdapter` using `CoreWebView2.CapturePreviewAsync`

## Platform Adapters — macOS
- [x] Add native shim function `ag_wk_capture_screenshot` in `WkWebViewShim.mm`
- [x] Add P/Invoke + C# handler in `MacOSWebViewAdapter`

## Platform Adapters — iOS
- [x] Add native shim function `ag_wk_capture_screenshot` in `WkWebViewShim.iOS.mm`
- [x] Add P/Invoke + C# handler in `iOSWebViewAdapter`
- [x] Rebuild iOS native libraries

## Platform Adapters — GTK
- [x] Add native shim function `ag_gtk_capture_screenshot` in `WebKitGtkShim.c`
- [x] Add P/Invoke + C# handler in `GtkWebViewAdapter`

## Platform Adapters — Android
- [x] Implement `IScreenshotAdapter` using `WebView.draw()` + `Bitmap.compress()`

## Consumer Surface
- [x] Add `CaptureScreenshotAsync()` to `WebView` control
- [x] Add `CaptureScreenshotAsync()` to `WebDialog` / `AvaloniaWebDialog`

## Tests
- [x] Add `IScreenshotAdapter` facet detection test
- [x] Add `CaptureScreenshotAsync()` contract test (throws when unsupported)
- [x] Add mock adapter with screenshot support test

## Build & Coverage
- [x] Verify all tests pass
- [x] Verify coverage >= 90%
