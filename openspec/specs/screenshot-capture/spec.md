## Requirements

### Requirement: IWebView includes CaptureScreenshotAsync
The `IWebView` interface SHALL define:
- `Task<byte[]> CaptureScreenshotAsync()`

The method SHALL return a PNG-encoded byte array of the current visible viewport.

#### Scenario: CaptureScreenshotAsync returns PNG data
- **WHEN** `CaptureScreenshotAsync()` is called on a loaded WebView
- **THEN** it returns a non-empty byte array starting with PNG magic bytes (0x89504E47)

### Requirement: IScreenshotAdapter facet for adapters
The adapter abstractions SHALL define an `IScreenshotAdapter` interface:
- `Task<byte[]> CaptureScreenshotAsync()`

The runtime SHALL detect `IScreenshotAdapter` via type check at initialization.

#### Scenario: Adapter implementing IScreenshotAdapter enables screenshots
- **WHEN** an adapter implements both `IWebViewAdapter` and `IScreenshotAdapter`
- **THEN** `CaptureScreenshotAsync()` delegates to the adapter

#### Scenario: Adapter without IScreenshotAdapter throws
- **WHEN** an adapter does not implement `IScreenshotAdapter`
- **THEN** `CaptureScreenshotAsync()` throws `NotSupportedException`

### Requirement: All platform adapters implement IScreenshotAdapter
All five platform adapters SHALL implement `IScreenshotAdapter` using native snapshot APIs:
- Windows: `CoreWebView2.CapturePreviewAsync`
- macOS: `WKWebView.takeSnapshot`
- iOS: `WKWebView.takeSnapshot`
- GTK: `webkit_web_view_get_snapshot`
- Android: `WebView.draw` to Bitmap

#### Scenario: Each adapter captures screenshot
- **WHEN** `CaptureScreenshotAsync()` is called on any platform
- **THEN** it returns valid PNG bytes representing the WebView content

### Requirement: WebView control exposes CaptureScreenshotAsync
The `WebView` Avalonia control and `WebDialog` SHALL expose `CaptureScreenshotAsync()`.

#### Scenario: Consumer captures screenshot from WebView control
- **WHEN** `await webView.CaptureScreenshotAsync()` is called
- **THEN** it returns PNG bytes
