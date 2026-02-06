## 1. Core contract types (WebViewContracts.cs)

- [x] 1.1 Define `WebViewCookie` sealed record with Name, Value, Domain, Path, Expires, IsSecure, IsHttpOnly
- [x] 1.2 Evolve `ICookieManager` from empty placeholder to full CRUD API: `GetCookiesAsync`, `SetCookieAsync`, `DeleteCookieAsync`, `ClearAllCookiesAsync` (keep `[Experimental("AGWV001")]`)
- [x] 1.3 Add `WebViewNetworkException` subclass of `WebViewNavigationException`
- [x] 1.4 Add `WebViewSslException` subclass of `WebViewNavigationException`
- [x] 1.5 Add `WebViewTimeoutException` subclass of `WebViewNavigationException`
- [x] 1.6 Add `Task NavigateToStringAsync(string html, Uri? baseUrl)` to `IWebView`

## 2. Adapter abstraction (IWebViewAdapter)

- [x] 2.1 Add `Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)` to `IWebViewAdapter`
- [x] 2.2 Define `ICookieAdapter` interface with `GetCookiesAsync`, `SetCookieAsync`, `DeleteCookieAsync`, `ClearAllCookiesAsync`

## 3. Runtime (WebViewCore)

- [x] 3.1 Implement `NavigateToStringAsync(string html, Uri? baseUrl)` — set `Source` to `baseUrl ?? about:blank`, raise `NavigationStarted` with matching `RequestUri`, delegate to adapter's new overload
- [x] 3.2 Make existing `NavigateToStringAsync(string html)` delegate to the new overload with `baseUrl: null`
- [x] 3.3 Detect `ICookieAdapter` on adapter at initialization; create and cache a runtime `ICookieManager` wrapper that adds dispatcher marshaling and lifecycle guards
- [x] 3.4 Update `TryGetCookieManager()` to return the cached `ICookieManager` (or null if adapter lacks `ICookieAdapter`)
- [x] 3.5 Implement `NewWindowRequested` unhandled fallback: after raising the event, if `Handled == false` and `Uri != null`, call `NavigateAsync(e.Uri)`
- [x] 3.6 Ensure error categorization pass-through: when adapter reports `NavigationCompleted` with a categorized exception subclass, preserve the specific type in the public `NavigationCompleted` event and in the faulted Task

## 4. macOS adapter — NavigateToStringAsync baseUrl

- [x] 4.1 Update `MacOSWebViewAdapter.NavigateToStringAsync` to accept the `baseUrl` parameter and pass it to `NativeMethods.LoadHtml`
- [x] 4.2 Add the two-parameter overload delegating to the three-parameter version with `baseUrl: null`

## 5. macOS adapter — ICookieAdapter via WKHTTPCookieStore

- [x] 5.1 Add native shim C ABI functions: `CookiesGet(handle, url, callback)`, `CookieSet(handle, json, callback)`, `CookieDelete(handle, json, callback)`, `CookiesClearAll(handle, callback)`
- [x] 5.2 Implement `CookiesGet` in ObjC++ shim: access `WKWebsiteDataStore.defaultDataStore.httpCookieStore`, filter by URL, marshal `NSHTTPCookie` fields to C callback
- [x] 5.3 Implement `CookieSet` in ObjC++ shim: construct `NSHTTPCookie` from parameters, call `setCookie:completionHandler:`
- [x] 5.4 Implement `CookieDelete` in ObjC++ shim: find matching cookie, call `deleteCookie:completionHandler:`
- [x] 5.5 Implement `CookiesClearAll` in ObjC++ shim: iterate `getAllCookies`, delete each, call completion
- [x] 5.6 Implement `ICookieAdapter` in `MacOSWebViewAdapter.PInvoke.cs`: P/Invoke wrappers with `TaskCompletionSource` for async completion, marshal `WebViewCookie` ↔ native fields
- [x] 5.7 Add lifecycle guards: throw `InvalidOperationException` if not attached, `ObjectDisposedException` if detached

## 6. macOS adapter — INativeWebViewHandleProvider

- [x] 6.1 Add native shim function `GetWebViewHandle(handle)` returning the `WKWebView*` pointer
- [x] 6.2 Implement `INativeWebViewHandleProvider` on `MacOSWebViewAdapter`: wrap pointer in `PlatformHandle("WKWebView")`

## 7. macOS adapter — Error categorization

- [x] 7.1 Extend native shim `map_error_status`: map `NSURLErrorTimedOut` → status 3, network errors (`-1003`, `-1004`, `-1005`, `-1009`) → status 4, SSL errors (`-1201` to `-1204`) → status 5
- [x] 7.2 Update `MacOSWebViewAdapter.OnNavigationCompletedNative` to construct `WebViewTimeoutException` (status 3), `WebViewNetworkException` (status 4), `WebViewSslException` (status 5) instead of generic `Exception`

## 8. Testing harness (MockWebViewAdapter)

- [x] 8.1 Add `ICookieAdapter` implementation to `MockWebViewAdapter` with in-memory `Dictionary` store, toggleable via constructor flag
- [x] 8.2 Add `RaiseNewWindowRequested(Uri? uri)` helper method
- [x] 8.3 Add `RaiseNavigationCompleted` overloads accepting `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`
- [x] 8.4 Record `LastBaseUrl` property when `NavigateToStringAsync(navigationId, html, baseUrl)` is called
- [x] 8.5 Implement `NavigateToStringAsync` three-parameter overload on mock, delegating from existing two-parameter version

## 9. Contract tests (CT)

- [x] 9.1 CT: `TryGetCookieManager()` returns non-null when mock has `ICookieAdapter`, null otherwise
- [x] 9.2 CT: Cookie CRUD via mock store — set, get, delete, clear operations
- [x] 9.3 CT: Cookie operation after dispose throws `ObjectDisposedException`
- [x] 9.4 CT: `NewWindowRequested` unhandled with non-null URI triggers `NavigateAsync` fallback
- [x] 9.5 CT: `NewWindowRequested` with `Handled=true` does not trigger fallback navigation
- [x] 9.6 CT: `NewWindowRequested` with null URI and unhandled takes no action
- [x] 9.7 CT: Navigation failure with `WebViewNetworkException` preserves type in `NavigationCompleted.Error`
- [x] 9.8 CT: Navigation failure with `WebViewSslException` preserves type in `NavigationCompleted.Error`
- [x] 9.9 CT: Navigation failure with `WebViewTimeoutException` preserves type in `NavigationCompleted.Error`
- [x] 9.10 CT: `NavigateToStringAsync(html, baseUrl)` sets `Source` to `baseUrl` and raises `NavigationStarted` with `RequestUri == baseUrl`
- [x] 9.11 CT: `NavigateToStringAsync(html, null)` preserves `about:blank` semantics
- [x] 9.12 CT: `NavigateToStringAsync(html)` delegates to overload with `baseUrl: null` (mock records `LastBaseUrl == null`)

## 10. Integration tests (IT) — macOS

- [x] 10.1 IT: Set a cookie via `ICookieManager`, navigate to a page that echoes `document.cookie`, verify cookie is present
- [x] 10.2 IT: Get cookies after page sets `document.cookie`, verify `GetCookiesAsync` returns the cookie
- [x] 10.3 IT: Delete a cookie and verify it no longer appears in `GetCookiesAsync`
- [x] 10.4 IT: `ClearAllCookiesAsync` and verify empty result (merged into 10.3)
- [x] 10.5 IT: `TryGetWebViewHandle()` returns non-null handle with descriptor `"WKWebView"` on macOS
- [x] 10.6 IT: Navigate to an invalid host, verify `NavigationCompleted.Error` is `WebViewNetworkException`
- [x] 10.7 IT: `NavigateToStringAsync(html, baseUrl)` verify baseUrl load and heading content

## 11. Compatibility matrix update

- [x] 11.1 Update compatibility matrix document with macOS M1 acceptance criteria (CT + IT mapping per capability)
- [x] 11.2 Add cookie management capability entry with macOS supported / other platforms not yet supported
- [x] 11.3 Add error categorization capability entry with `NSURLError` mapping notes

## 12. Verification

- [x] 12.1 Run full CT suite and confirm all existing + new tests pass (84/84 passed)
- [ ] 12.2 Run macOS IT suite and confirm deterministic pass for M1 scenarios (requires manual macOS app run)
- [ ] 12.3 Verify existing M0 IT smoke tests remain green (requires manual macOS app run)
