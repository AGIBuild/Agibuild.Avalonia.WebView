## 1. Project setup

- [x] 1.1 Update `Agibuild.Fulora.Adapters.Android.csproj`: add `CA2255` NoWarn for ModuleInitializer
- [x] 1.2 Add `AndroidAdapterModule` with `[ModuleInitializer]` registering `AndroidWebViewAdapter` in `WebViewAdapterRegistry` (guard `OperatingSystem.IsAndroid()`)

## 2. Adapter lifecycle (Initialize / Attach / Detach)

- [x] 2.1 Implement `Initialize(IWebViewAdapterHost host)`: store host reference, validate single-call
- [x] 2.2 Implement `Attach(IPlatformHandle parentHandle)`: resolve parent to Android `ViewGroup`, create `android.webkit.WebView` with Activity context, configure `WebSettings`, attach `WebViewClient` and `WebChromeClient`, add to parent
- [x] 2.3 Implement `Detach()`: remove WebView from parent, call `WebView.Destroy()`, clean up state
- [x] 2.4 Implement lifecycle guards: throw `InvalidOperationException` if not initialized/attached, `ObjectDisposedException` if detached

## 3. Navigation — API-initiated

- [x] 3.1 Implement `NavigateAsync(Guid navigationId, Uri uri)`: track the API-issued `navigationId`, call `WebView.LoadUrl(uri)`
- [x] 3.2 Implement `NavigateToStringAsync(Guid navigationId, string html)`: call `WebView.LoadData(html, "text/html", "UTF-8")`
- [x] 3.3 Implement `NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)`: when `baseUrl` is non-null, call `WebView.LoadDataWithBaseURL(baseUrl, html, "text/html", "UTF-8", null)`; when null delegate to the two-parameter overload

## 4. Navigation — native-initiated interception

- [x] 4.1 Override `WebViewClient.ShouldOverrideUrlLoading(WebView, IWebResourceRequest)`: for native-initiated navigations (not API-tracked), call `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)` with `CorrelationId`
- [x] 4.2 Honor the host decision: if `IsAllowed == false`, return `true` from `ShouldOverrideUrlLoading` to cancel
- [x] 4.3 Implement `CorrelationId` tracking: maintain current correlation state; reuse correlation for redirect chains detected via `OnPageStarted` URL changes

## 5. Navigation — completion and error mapping

- [x] 5.1 Override `WebViewClient.OnPageFinished(WebView, String)`: resolve the `NavigationId` (API-tracked or host-issued), raise adapter `NavigationCompleted` event with `Success` status
- [x] 5.2 Override `WebViewClient.OnReceivedError(WebView, IWebResourceRequest, WebResourceError)`: map `WebResourceError.ErrorCode` to exception hierarchy — `ERROR_HOST_LOOKUP`/`ERROR_CONNECT`/`ERROR_IO` → `WebViewNetworkException`; `ERROR_FAILED_SSL_HANDSHAKE` → `WebViewSslException`; `ERROR_TIMEOUT` → `WebViewTimeoutException`; others → `WebViewNavigationException`
- [x] 5.3 Ensure exactly-once completion: guard against duplicate completion events per `NavigationId`
- [x] 5.4 Clean up navigation state on completion

## 6. Navigation — commands

- [x] 6.1 Implement `GoBack(Guid navigationId)`: check `CanGoBack`, call `WebView.GoBack()`, track as API-initiated
- [x] 6.2 Implement `GoForward(Guid navigationId)`: check `CanGoForward`, call `WebView.GoForward()`, track as API-initiated
- [x] 6.3 Implement `Refresh(Guid navigationId)`: call `WebView.Reload()`, track as API-initiated
- [x] 6.4 Implement `Stop()`: call `WebView.StopLoading()`
- [x] 6.5 Implement `CanGoBack` / `CanGoForward` properties delegating to `WebView`

## 7. Script execution

- [x] 7.1 Implement `InvokeScriptAsync(string script)`: call `WebView.EvaluateJavascript(script, IValueCallback)`, return the result string via `TaskCompletionSource`

## 8. WebMessage bridge (receive path)

- [x] 8.1 Create `AndroidJsBridge` class with `[JavascriptInterface]`-annotated method to receive `postMessage` calls from JavaScript
- [x] 8.2 At `Attach` time, call `WebView.AddJavascriptInterface(bridge, channelName)` to inject the bridge object
- [x] 8.3 In `WebViewClient.OnPageStarted`, inject channel-routing script to establish `window.chrome.webview.postMessage` → bridge routing
- [x] 8.4 On bridge message receipt, extract origin, body, and channel metadata; raise adapter `WebMessageReceived` event

## 9. NewWindowRequested

- [x] 9.1 Override `WebChromeClient.OnCreateWindow(WebView, bool, bool, Message)`: extract target URI, raise adapter `NewWindowRequested` event, respect `Handled` flag

## 10. ICookieAdapter via android.webkit.CookieManager

- [x] 10.1 Implement `GetCookiesAsync(Uri uri)`: call `CookieManager.GetCookie(uri)`, parse cookie header string into `WebViewCookie` objects
- [x] 10.2 Implement `SetCookieAsync(WebViewCookie cookie)`: format cookie string, call `CookieManager.SetCookie(url, cookieString)` with `IValueCallback` for async completion
- [x] 10.3 Implement `DeleteCookieAsync(WebViewCookie cookie)`: set cookie with `expires` in the past via `SetCookie`
- [x] 10.4 Implement `ClearAllCookiesAsync()`: call `CookieManager.RemoveAllCookies(IValueCallback)`
- [x] 10.5 Add lifecycle guards: throw `InvalidOperationException` if not attached, `ObjectDisposedException` if detached

## 11. INativeWebViewHandleProvider

- [x] 11.1 Implement `TryGetWebViewHandle()`: return Android `WebView` instance wrapped in `PlatformHandle("AndroidWebView")`

## 12. IWebViewAdapterOptions

- [x] 12.1 Implement `ApplyEnvironmentOptions(IWebViewEnvironmentOptions options)`: store options for use during `Attach` (DevTools toggle via `WebView.SetWebContentsDebuggingEnabled`)
- [x] 12.2 Implement `SetCustomUserAgent(string? userAgent)`: apply via `WebSettings.UserAgentString`

## 13. Compatibility matrix update

- [x] 13.1 Update compatibility matrix document with Android M0 acceptance criteria (CT + IT mapping)
- [x] 13.2 Update cookie management capability entry to include Android as supported
- [x] 13.3 Update error categorization capability entry with `WebViewClient.ERROR_*` mapping notes

## 14. Verification

- [x] 14.1 Run full CT suite and confirm all existing tests remain green (224/224 passed)
- [ ] 14.2 Run Android IT suite and confirm deterministic pass for M0 scenarios (requires Android device/emulator)
- [ ] 14.3 Verify macOS and Windows IT tests remain unaffected (no regression)
