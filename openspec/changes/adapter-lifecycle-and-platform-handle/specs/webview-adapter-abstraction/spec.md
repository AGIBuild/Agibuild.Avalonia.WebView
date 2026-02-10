## MODIFIED Requirements

### Requirement: INativeWebViewHandleProvider is implementable by adapters
Platform adapters MAY implement `INativeWebViewHandleProvider` to expose the underlying native WebView handle.
The `HandleDescriptor` property of the returned `IPlatformHandle` SHALL identify the native type (e.g., `"WKWebView"`, `"WebView2"`).
The returned `IPlatformHandle` SHALL also implement the appropriate typed platform handle interface from Core:
- Windows adapters: `IWindowsWebView2PlatformHandle`
- macOS/iOS adapters: `IAppleWKWebViewPlatformHandle`
- GTK adapters: `IGtkWebViewPlatformHandle`
- Android adapters: `IAndroidWebViewPlatformHandle`

#### Scenario: macOS adapter exposes WKWebView handle
- **WHEN** the macOS adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "WKWebView"`
- **AND** the handle implements `IAppleWKWebViewPlatformHandle`

#### Scenario: Windows adapter exposes WebView2 handle
- **WHEN** the Windows adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "WebView2"`
- **AND** the handle implements `IWindowsWebView2PlatformHandle`

#### Scenario: Android adapter exposes Android WebView handle
- **WHEN** the Android adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "AndroidWebView"`
- **AND** the handle implements `IAndroidWebViewPlatformHandle`

#### Scenario: GTK adapter exposes WebKitGTK handle
- **WHEN** the GTK adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "WebKitGTK"`
- **AND** the handle implements `IGtkWebViewPlatformHandle`
