## Why

The macOS (WKWebView) and Windows (WebView2) adapters have validated the v1 contract semantics end-to-end and proven portability across desktop WebView engines. Android is the highest-traffic mobile platform and currently has only a stub adapter that throws `PlatformNotSupportedException`. Implementing the Android adapter unblocks mobile users and extends the contract to the `android.webkit.WebView` engine.

Because the contract surface is mature (cookie management, error categorization, native handle, baseUrl all defined), Android M0 can deliver full baseline + extended parity in a single milestone — Android's `android.webkit` APIs are well-supported via .NET for Android bindings.

## What Changes

- **Implement `AndroidWebViewAdapter`** backed by `android.webkit.WebView`, satisfying all `IWebViewAdapter` contract members:
  - Native-initiated navigation interception via `WebViewClient.shouldOverrideUrlLoading()`, gated by `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
  - Redirect correlation with stable `CorrelationId` across redirect chains (via `onPageStarted()` URL tracking)
  - `NavigationCompleted` with host-issued `NavigationId`, exactly-once semantics
  - `NavigateToStringAsync` with `baseUrl` support via `WebView.loadDataWithBaseURL()`
  - Script execution via `WebView.evaluateJavascript()`
  - WebMessage bridge receive path via `addJavascriptInterface()` with `@JavascriptInterface` annotation
  - `NewWindowRequested` event propagation via `WebChromeClient.onCreateWindow()`
- **Implement `ICookieAdapter`** via `android.webkit.CookieManager`
- **Implement `INativeWebViewHandleProvider`** exposing the Android `WebView` instance with descriptor `"AndroidWebView"`
- **Implement `IWebViewAdapterOptions`** for DevTools toggle (`WebView.setWebContentsDebuggingEnabled`) and custom UserAgent (`WebSettings.setUserAgentString`)
- **Implement navigation error categorization**: map `WebViewClient.onReceivedError()` error codes to `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`
- **Register via `ModuleInitializer`** following the Windows `WindowsAdapterModule` pattern
- **Add Android IT smoke tests** mirroring macOS/Windows IT scope: link click, redirect correlation, cancellation, script + WebMessage, cookie CRUD, error categorization, native handle
- **Update compatibility matrix** with Android M0 acceptance criteria

## Capabilities

### New Capabilities

- (none — all contract surfaces are already defined)

### Modified Capabilities

- `webview-compatibility-matrix`: add Android M0 acceptance criteria (CT + IT mapping per capability)
- `webview-testing-harness`: add Android-only IT smoke requirements mirroring macOS/Windows IT coverage

## Impact

- **Affected code**: `Agibuild.Avalonia.WebView.Adapters.Android` — full adapter implementation replacing the current stub
- **No new NuGet dependencies**: Android WebView APIs are available via .NET for Android platform bindings
- **csproj change**: Android adapter keeps `net10.0-android` TFM, adds `CA2255` NoWarn for ModuleInitializer
- **Public API surface**: no changes — contracts are already defined; this change implements them on Android
- **Integration tests**: existing Android IT project (`Agibuild.Avalonia.WebView.Integration.Tests.Android`) already scaffolded
- **No breaking changes**: additive implementation only
