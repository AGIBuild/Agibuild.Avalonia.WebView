## Context

The v1 contract semantics have been validated end-to-end via the macOS WKWebView adapter (M0 + M1) and the Windows WebView2 adapter (M0). The Android adapter (`AndroidWebViewAdapter`) is currently a stub that throws `PlatformNotSupportedException` for all operations. The adapter registry infrastructure (`WebViewAdapterRegistry`, `ModuleInitializer` pattern) is proven on both desktop platforms.

Android's `android.webkit.WebView` is the native WebView engine for Android devices. Unlike the Windows adapter (which uses a managed NuGet SDK) or the macOS adapter (which requires a custom ObjC++ native shim with P/Invoke), the Android adapter uses .NET for Android bindings that directly expose the Java `android.webkit` API surface.

The contract surfaces are fully defined: `IWebViewAdapter`, `ICookieAdapter`, `INativeWebViewHandleProvider`, `IWebViewAdapterOptions`, error categorization exceptions, and baseUrl overloads.

Constraints:
- Android WebView must be created and manipulated on the UI (main) thread.
- `android.webkit.WebView` requires a `Context` (Activity or Application context) for instantiation.
- The adapter's `Attach(IPlatformHandle parentHandle)` receives a reference that must be resolved to an Android `ViewGroup` for adding the WebView as a child view.
- Server-side 302 redirects do NOT trigger `shouldOverrideUrlLoading()` — redirect correlation requires an alternative tracking mechanism.
- `android.webkit.CookieManager` is a process-wide singleton with a string-based API (no structured cookie object).

## Goals / Non-Goals

**Goals:**
- Implement `AndroidWebViewAdapter` backed by `android.webkit.WebView` covering all `IWebViewAdapter` members.
- Implement optional facets: `ICookieAdapter`, `INativeWebViewHandleProvider`, `IWebViewAdapterOptions`.
- Map `WebViewClient.onReceivedError()` error codes to the error categorization hierarchy (`WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`).
- Support `NavigateToStringAsync` with `baseUrl` parameter via `loadDataWithBaseURL()`.
- Register via `ModuleInitializer` following the Windows/macOS pattern.
- Provide Android IT smoke tests covering the same scope as macOS/Windows IT.

**Non-Goals:**
- Custom scheme handlers or `WebViewClient.shouldInterceptRequest()` interception beyond basic adapter needs.
- Full `window.open` lifecycle management — `NewWindowRequested` event propagation only.
- WebView hardware acceleration or rendering customization.
- Supporting Android API levels below 21 (Lollipop).
- Multi-process WebView configuration or crash recovery (`RenderProcessGoneDetail`).

## Decisions

### 1) Direct .NET for Android bindings

**Decision:** Use the `Android.Webkit` namespace from .NET for Android bindings directly. No additional NuGet packages required.

**Rationale:**
- .NET for Android provides complete bindings for `android.webkit.WebView`, `WebViewClient`, `WebChromeClient`, and `CookieManager`.
- The bindings are part of the platform SDK — no extra dependencies needed.
- APIs are well-documented and stable since API level 21+.

**Alternatives considered:**
- Use Chromium-based custom WebView (e.g., GeckoView, Crosswalk): rejected as unnecessary complexity when the system WebView is sufficient and updated via Google Play.

### 2) Lifecycle — WebView creation in Attach

**Decision:** Perform Android WebView creation in the `Attach` method:
1. `Initialize(host)` → store host reference, validate single-call.
2. `Attach(parentHandle)` → resolve the `IPlatformHandle` to an Android `ViewGroup`, create `android.webkit.WebView` with the Activity context, add as child view, configure settings, attach `WebViewClient` and `WebChromeClient`.
3. `Detach()` → remove WebView from parent, call `WebView.destroy()`, clean up state.

**Rationale:**
- Android `WebView` requires a `Context` and a parent `ViewGroup` for proper lifecycle. This maps to the `Attach(parentHandle)` call.
- Consistent with the Windows adapter which creates the WebView2 controller in `Attach`.

**Alternatives considered:**
- Create WebView eagerly in Initialize with Application context: rejected because WebView requires Activity context for proper theming and lifecycle integration.

### 3) Navigation interception via shouldOverrideUrlLoading

**Decision:** Override `WebViewClient.shouldOverrideUrlLoading(WebView, WebResourceRequest)` to intercept native-initiated navigations. For each intercepted request:
1. Check if navigation is API-initiated (tracked) → allow immediately.
2. If native-initiated → call `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)` with a `CorrelationId`.
3. Honor the host decision: if denied, return `true` from `shouldOverrideUrlLoading` to cancel.

**Rationale:**
- `shouldOverrideUrlLoading` is the standard interception point for user-initiated navigations (link clicks, form submissions).
- Returning `true` cancels the navigation, matching the `Cancel` semantics in the contract.

**Alternatives considered:**
- Use `onPageStarted` for interception: rejected because it fires after navigation begins, too late to cancel.

### 4) Redirect correlation strategy

**Decision:** Android's `shouldOverrideUrlLoading()` is NOT called for server-side redirects (302/301). To track redirects:
- Use `onPageStarted(WebView, String url, Bitmap favicon)` to detect URL changes during an active navigation.
- Maintain a `_currentCorrelationId` that persists across a single navigation chain.
- A new `shouldOverrideUrlLoading` call creates a new correlation; `onPageStarted` within an active navigation reuses the existing correlation.
- Navigation completion (`onPageFinished`) clears the active chain.

**Rationale:**
- Android WebView does not expose redirect details at the `WebViewClient` level for server-side redirects.
- Tracking via `onPageStarted` URL changes provides sufficient redirect awareness for the CorrelationId contract (stable ID across chain).
- This mirrors the practical limitation of the platform — perfect redirect detection is not possible without `shouldInterceptRequest`.

**Alternatives considered:**
- Use `shouldInterceptRequest` to detect redirects: rejected as overly invasive (intercepts all resource loads, not just navigations) and complicates the adapter.
- Ignore redirect correlation entirely: rejected because the contract requires stable CorrelationId across chains.

### 5) Error categorization mapping

**Decision:** Map Android `WebViewClient.onReceivedError()` error codes (`WebViewClient.ERROR_*`) to exception types:

| Android ErrorCode | Exception Type |
|---|---|
| `ERROR_HOST_LOOKUP`, `ERROR_CONNECT`, `ERROR_IO`, `ERROR_UNKNOWN` | `WebViewNetworkException` |
| `ERROR_FAILED_SSL_HANDSHAKE`, `ERROR_AUTHENTICATION` | `WebViewSslException` |
| `ERROR_TIMEOUT` | `WebViewTimeoutException` |
| All other errors | `WebViewNavigationException` (base) |

**Rationale:**
- Android error codes map well to the existing exception hierarchy.
- `ERROR_AUTHENTICATION` is grouped with SSL because Android reports it for client certificate failures.
- `ERROR_UNKNOWN` is mapped to network errors as it typically indicates connectivity issues.

### 6) NavigateToStringAsync with baseUrl

**Decision:** Use `WebView.loadDataWithBaseURL(baseUrl, html, "text/html", "UTF-8", null)` when `baseUrl` is non-null. Use `WebView.loadData(html, "text/html", "UTF-8")` when `baseUrl` is null.

**Rationale:**
- Android `WebView.loadDataWithBaseURL()` natively supports the `baseUrl` parameter — no intercept hacks needed.
- This is simpler than both the WebView2 approach (WebResourceRequested intercept) and the WKWebView approach.
- The `historyUrl` parameter (last arg) is set to `null` to avoid polluting history.

### 7) Cookie management via android.webkit.CookieManager

**Decision:** Implement `ICookieAdapter` using the `android.webkit.CookieManager` singleton:
- `GetCookiesAsync(uri)` → `CookieManager.GetCookie(uri.ToString())`, parse the cookie header string into `WebViewCookie` objects.
- `SetCookieAsync(cookie)` → `CookieManager.SetCookie(url, cookieString)` with `TaskCompletionSource` wrapping the `IValueCallback`.
- `DeleteCookieAsync(cookie)` → set cookie with `expires` in the past via `SetCookie`.
- `ClearAllCookiesAsync()` → `CookieManager.RemoveAllCookies(IValueCallback)`.

**Rationale:**
- `CookieManager` is the only API for cookie manipulation on Android WebView.
- Cookie strings must be parsed/formatted manually since Android uses raw `Set-Cookie` header format.
- `RemoveAllCookies` accepts a callback, enabling proper async wrapping.

**Alternatives considered:**
- Use `CookieManager.RemoveSessionCookies` + `RemoveAllCookies`: rejected for `DeleteCookieAsync` because there's no selective delete API — the expired-cookie trick is the standard Android approach.

### 8) WebMessage bridge (receive path)

**Decision:** Use `WebView.addJavascriptInterface(Object, String)` to inject a Java bridge object with a `@JavascriptInterface`-annotated method. The bridge object receives `postMessage` calls from JavaScript and raises the `WebMessageReceived` event on the adapter.

The channel-routing script is injected via `WebViewClient.onPageStarted()` to establish `window.agibuild.webview.postMessage` routing to the injected bridge.

**Rationale:**
- `addJavascriptInterface` is the standard mechanism for JS-to-native communication on Android.
- `@JavascriptInterface` annotation is required since API 17+ for security.
- Injection at `onPageStarted` ensures the bridge is available when page scripts execute.

**Alternatives considered:**
- Use `WebMessagePort` (API 23+): rejected to maintain API 21 compatibility.
- Use `WebView.evaluateJavascript` polling: rejected as inefficient and non-reactive.

### 9) Native handle provider

**Decision:** Implement `INativeWebViewHandleProvider` returning the `android.webkit.WebView` instance wrapped in a `PlatformHandle("AndroidWebView")`.

**Rationale:**
- The `WebView` object is the primary entry point for consumers needing advanced Android-specific operations.
- Descriptor `"AndroidWebView"` follows the pattern of `"WKWebView"` and `"WebView2"`.

### 10) Module initializer registration

**Decision:** Add `AndroidAdapterModule` with `[ModuleInitializer]` that calls `WebViewAdapterRegistry.Register(...)` when `OperatingSystem.IsAndroid()`, following the Windows/macOS pattern.

**Rationale:**
- Consistent with the existing adapter-as-plugin registration pattern.
- No changes needed to the registry infrastructure.

## Risks / Trade-offs

- **[shouldOverrideUrlLoading not called for server-side redirects]** Android's WebViewClient does not invoke `shouldOverrideUrlLoading` for 301/302 redirects. → Mitigation: use `onPageStarted` URL tracking for redirect chain correlation. This provides weaker redirect visibility than WebView2/WKWebView but satisfies the CorrelationId stability contract.
- **[CookieManager is process-wide singleton]** All WebView instances share the same cookie jar. → Mitigation: document this platform difference. Per-instance cookie isolation is not possible on Android.
- **[CookieManager string parsing]** `GetCookie()` returns a semicolon-delimited string without domain/path/expiry metadata. → Mitigation: `GetCookiesAsync` returns cookies with name+value only; domain is inferred from the request URI. This is a known Android limitation.
- **[UI thread requirement]** All Android WebView operations must execute on the main thread. → Mitigation: adapter methods post to `Looper.MainLooper` via `Handler`; the runtime already marshals via dispatcher.
- **[addJavascriptInterface security]** Prior to API 17, `addJavascriptInterface` had a remote code execution vulnerability. → Mitigation: minimum API level is 21, well above the fix threshold.
- **[loadDataWithBaseURL encoding]** Large HTML strings may hit Android's URL length limits when base64-encoded internally. → Mitigation: use `"UTF-8"` encoding parameter which avoids base64 for `loadDataWithBaseURL`.
