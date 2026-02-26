## Why

The macOS WKWebView adapter (M0 + M1) has validated the v1 contract semantics end-to-end and established the adapter implementation pattern. Windows is the highest-traffic desktop platform and currently has only a stub adapter that throws `PlatformNotSupportedException`. Implementing the Windows WebView2 adapter unblocks Windows users and proves the contract abstractions are portable across fundamentally different WebView engines.

Because the contract surface is now mature (cookie management, error categorization, native handle, baseUrl all defined post-M1), the Windows M0 can deliver full baseline + extended parity in a single milestone — WebView2's managed .NET SDK makes these capabilities straightforward compared to the WKWebView P/Invoke approach.

## What Changes

- **Implement `WindowsWebViewAdapter`** backed by Microsoft WebView2, satisfying all `IWebViewAdapter` contract members:
  - Native-initiated navigation interception via WebView2 `NavigationStarting` event, gated by `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
  - Redirect correlation with stable `CorrelationId` across redirect chains
  - `NavigationCompleted` with host-issued `NavigationId`, exactly-once semantics
  - `NavigateToStringAsync` with `baseUrl` support (via `NavigateToString` + `SetVirtualHostNameToFolderMapping` or `NavigateWithWebResourceRequest`)
  - Script execution via `ExecuteScriptAsync`
  - WebMessage bridge receive path via `WebMessageReceived`
  - `NewWindowRequested` event propagation
- **Implement `ICookieAdapter`** via WebView2's `CoreWebView2CookieManager`
- **Implement `INativeWebViewHandleProvider`** exposing the `CoreWebView2Controller` handle with descriptor `"WebView2"`
- **Implement `IWebViewAdapterOptions`** for DevTools toggle and custom UserAgent
- **Implement navigation error categorization**: map `CoreWebView2WebErrorStatus` to `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`
- **Register via `ModuleInitializer`** following the macOS `MacOSAdapterModule` pattern
- **Add Windows IT smoke tests** mirroring macOS IT scope: link click, redirect correlation, cancellation, script + WebMessage, cookie CRUD, error categorization, native handle
- **Update compatibility matrix** with Windows/WebView2 acceptance criteria

## Capabilities

### New Capabilities

- (none — all contract surfaces are already defined)

### Modified Capabilities

- `webview-compatibility-matrix`: add Windows/WebView2 M0 acceptance criteria (CT + IT mapping per capability)
- `webview-testing-harness`: add Windows-only IT smoke requirements mirroring macOS IT coverage

## Impact

- **Affected code**: `Agibuild.Fulora.Adapters.Windows` — full adapter implementation replacing the current stub
- **New dependency**: `Microsoft.Web.WebView2` NuGet package (MIT license)
- **csproj change**: Windows adapter targets `net10.0-windows10.0.17763.0` (or newer) to access WinRT WebView2 APIs
- **Public API surface**: no changes — contracts are already defined; this change implements them on Windows
- **Integration tests**: new Windows-only IT project or conditional tests in existing IT project
- **No breaking changes**: additive implementation only
