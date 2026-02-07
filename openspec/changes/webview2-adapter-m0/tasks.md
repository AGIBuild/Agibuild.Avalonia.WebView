## 1. Project setup

- [x] 1.1 Update `Agibuild.Avalonia.WebView.Adapters.Windows.csproj`: add `Microsoft.Web.WebView2` NuGet package reference (kept TFM as `net10.0` for cross-platform buildability, consistent with macOS adapter)
- [x] 1.2 Add `WindowsAdapterModule` with `[ModuleInitializer]` registering `WindowsWebViewAdapter` in `WebViewAdapterRegistry` (guard `OperatingSystem.IsWindows()`)

## 2. Adapter lifecycle (Initialize / Attach / Detach)

- [x] 2.1 Implement `Initialize(IWebViewAdapterHost host)`: store host reference, validate single-call
- [x] 2.2 Implement `Attach(IPlatformHandle parentHandle)`: create `CoreWebView2Environment`, then `CoreWebView2Controller` parented to the HWND; subscribe to WebView2 events; queue navigation requests received before readiness
- [x] 2.3 Implement `Detach()`: unsubscribe events, close `CoreWebView2Controller`, set detached guard flag
- [x] 2.4 Implement lifecycle guards: throw `InvalidOperationException` if not initialized/attached, `ObjectDisposedException` if detached

## 3. Navigation — API-initiated

- [x] 3.1 Implement `NavigateAsync(Guid navigationId, Uri uri)`: track the API-issued `navigationId`, call `CoreWebView2.Navigate(uri)`
- [x] 3.2 Implement `NavigateToStringAsync(Guid navigationId, string html)`: call `CoreWebView2.NavigateToString(html)`
- [x] 3.3 Implement `NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)`: when `baseUrl` is non-null, register temporary `WebResourceRequested` filter, intercept request to serve HTML, navigate to `baseUrl`; when null delegate to the two-parameter overload

## 4. Navigation — native-initiated interception

- [x] 4.1 Subscribe to `CoreWebView2.NavigationStarting`: for native-initiated navigations (not API-tracked), call `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)` with `CorrelationId` derived from WebView2's `NavigationId`
- [x] 4.2 Honor the host decision: if `IsAllowed == false`, set `e.Cancel = true`; track the host-issued `NavigationId` for later completion
- [x] 4.3 Implement `CorrelationId` mapping: maintain `Dictionary<ulong, Guid>` for WebView2 `NavigationId` → `CorrelationId`; reuse existing entry for redirects, create new entry for new chains
- [x] 4.4 Implement API-initiated navigation tracking: maintain a `HashSet<ulong>` of WebView2 `NavigationId` values that originated from adapter API calls (to skip host callback for these)

## 5. Navigation — completion and error mapping

- [x] 5.1 Subscribe to `CoreWebView2.NavigationCompleted`: resolve the `NavigationId` (API-tracked or host-issued), raise adapter `NavigationCompleted` event
- [x] 5.2 Map `CoreWebView2WebErrorStatus` to exception hierarchy: Timeout → `WebViewTimeoutException`; connection/DNS errors → `WebViewNetworkException`; certificate errors → `WebViewSslException`; other failures → `WebViewNavigationException`
- [x] 5.3 Ensure exactly-once completion: guard against duplicate completion events per `NavigationId`
- [x] 5.4 Clean up correlation state on completion

## 6. Navigation — commands

- [x] 6.1 Implement `GoBack(Guid navigationId)`: check `CanGoBack`, call `CoreWebView2.GoBack()`, track as API-initiated
- [x] 6.2 Implement `GoForward(Guid navigationId)`: check `CanGoForward`, call `CoreWebView2.GoForward()`, track as API-initiated
- [x] 6.3 Implement `Refresh(Guid navigationId)`: call `CoreWebView2.Reload()`, track as API-initiated
- [x] 6.4 Implement `Stop()`: call `CoreWebView2.Stop()`
- [x] 6.5 Implement `CanGoBack` / `CanGoForward` properties delegating to `CoreWebView2`

## 7. Script execution

- [x] 7.1 Implement `InvokeScriptAsync(string script)`: call `CoreWebView2.ExecuteScriptAsync(script)`, return the result string

## 8. WebMessage bridge (receive path)

- [x] 8.1 At `Attach` time, inject channel-routing script via `CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(...)` to route `window.chrome.webview.postMessage` to the adapter's channel
- [x] 8.2 Subscribe to `CoreWebView2.WebMessageReceived`: extract origin, body, and channel metadata; raise adapter `WebMessageReceived` event

## 9. NewWindowRequested

- [x] 9.1 Subscribe to `CoreWebView2.NewWindowRequested`: extract target URI, raise adapter `NewWindowRequested` event, respect `Handled` flag

## 10. ICookieAdapter via CoreWebView2CookieManager

- [x] 10.1 Implement `GetCookiesAsync(Uri uri)`: call `CookieManager.GetCookiesAsync(uri)`, map `CoreWebView2Cookie` → `WebViewCookie`
- [x] 10.2 Implement `SetCookieAsync(WebViewCookie cookie)`: call `CookieManager.CreateCookie(...)` + `CookieManager.AddOrUpdateCookie(...)`
- [x] 10.3 Implement `DeleteCookieAsync(WebViewCookie cookie)`: find matching cookie via `GetCookiesAsync`, call `CookieManager.DeleteCookie(...)`
- [x] 10.4 Implement `ClearAllCookiesAsync()`: call `CookieManager.DeleteAllCookies()`
- [x] 10.5 Add lifecycle guards: throw `InvalidOperationException` if not attached, `ObjectDisposedException` if detached

## 11. INativeWebViewHandleProvider

- [x] 11.1 Implement `TryGetWebViewHandle()`: return `CoreWebView2Controller` parent window handle wrapped in `PlatformHandle("WebView2")`

## 12. IWebViewAdapterOptions

- [x] 12.1 Implement `ApplyEnvironmentOptions(IWebViewEnvironmentOptions options)`: store options for use during `Attach` (DevTools toggle via `CoreWebView2Settings.AreDevToolsEnabled`)
- [x] 12.2 Implement `SetCustomUserAgent(string? userAgent)`: apply via `CoreWebView2Settings.UserAgent`

## 13. Windows IT smoke tests

- [ ] 13.1 Set up Windows-specific IT test infrastructure (loopback HTTP server, WebView2 test harness)
- [ ] 13.2 IT: Link click navigation — verify `NavigationStarted` and `NavigationCompleted` for the same `NavigationId`
- [ ] 13.3 IT: 302 redirect correlation — verify same `CorrelationId` across redirect chain and exactly-once completion
- [ ] 13.4 IT: `window.location` script-driven navigation — verify native interception and successful completion
- [ ] 13.5 IT: Cancellation via `Cancel=true` — verify native step denied and `NavigationCompleted` with `Canceled`
- [ ] 13.6 IT: Script execution + WebMessage receive round-trip
- [ ] 13.7 IT: Cookie CRUD — set, get, delete via `ICookieManager`, verify via page `document.cookie`
- [ ] 13.8 IT: Navigate to unreachable host, verify `WebViewNetworkException`
- [ ] 13.9 IT: `TryGetWebViewHandle()` returns non-null handle with descriptor `"WebView2"`
- [ ] 13.10 IT: `NavigateToStringAsync(html, baseUrl)` — verify baseUrl load and heading content

## 14. Compatibility matrix update

- [x] 14.1 Update compatibility matrix document with Windows/WebView2 M0 acceptance criteria (CT + IT mapping)
- [x] 14.2 Update cookie management capability entry to include Windows as supported
- [x] 14.3 Update error categorization capability entry with `CoreWebView2WebErrorStatus` mapping notes

## 15. Verification

- [x] 15.1 Run full CT suite and confirm all existing tests remain green (196/196 passed)
- [ ] 15.2 Run Windows IT suite and confirm deterministic pass for M0 scenarios (requires manual Windows app run)
- [ ] 15.3 Verify macOS IT tests remain unaffected (no regression)
