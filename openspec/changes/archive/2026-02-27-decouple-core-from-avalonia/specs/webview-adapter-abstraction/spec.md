## MODIFIED Requirements

### Requirement: IWebViewAdapter contract surface
The `IWebViewAdapter` interface SHALL define:
- lifecycle: `void Initialize(IWebViewAdapterHost host)`, `void Attach(INativeHandle parentHandle)`, `void Detach()`
- navigation: `Task NavigateAsync(Guid navigationId, Uri uri)`, `Task NavigateToStringAsync(Guid navigationId, string html)`, `Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)`
- scripting: `Task<string?> InvokeScriptAsync(string script)`
- commands: `bool GoBack(Guid navigationId)`, `bool GoForward(Guid navigationId)`, `bool Refresh(Guid navigationId)`, `bool Stop()`
- state: `bool CanGoBack { get; }`, `bool CanGoForward { get; }`
- events: `NavigationCompleted`, `NewWindowRequested`, `WebMessageReceived`, `WebResourceRequested`, `EnvironmentRequested`

#### Scenario: IWebViewAdapter members are available
- **WHEN** a consumer reflects on `IWebViewAdapter`
- **THEN** all listed members are present with the specified signatures

### Requirement: INativeWebViewHandleProvider is implementable by adapters
Platform adapters SHALL expose the underlying native WebView handle through `INativeWebViewHandleProvider` when native-handle access is supported.
The `HandleDescriptor` property of the returned `INativeHandle` SHALL identify the native type (e.g., `"WKWebView"`, `"WebView2"`).
The returned `INativeHandle` SHALL also implement the appropriate typed platform handle interface from Core:
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
