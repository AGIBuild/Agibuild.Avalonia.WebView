## Context

The v1 contract semantics have been validated end-to-end via the macOS WKWebView adapter (M0 + M1). The Windows adapter (`WindowsWebViewAdapter`) is currently a stub that throws `PlatformNotSupportedException` for all operations. The adapter registry infrastructure (`WebViewAdapterRegistry`, `ModuleInitializer` pattern) is already proven on macOS and ready to be reused.

Microsoft WebView2 provides a managed .NET SDK (`Microsoft.Web.WebView2`) with a rich API surface. Unlike the macOS adapter which required a custom ObjC++ native shim with P/Invoke, the Windows adapter can use the WebView2 SDK directly — no native code compilation or platform-specific build steps.

The contract surfaces are fully defined post-M1: `IWebViewAdapter`, `ICookieAdapter`, `INativeWebViewHandleProvider`, `IWebViewAdapterOptions`, error categorization exceptions, and baseUrl overloads. This adapter is purely implementation work against existing contracts.

Constraints:
- WebView2 requires the Evergreen Runtime to be installed on the target machine (ships with Windows 10/11 updates and Edge browser).
- WebView2 initialization is asynchronous (`CoreWebView2Environment.CreateAsync` → `CoreWebView2Controller.EnsureCoreWebView2Async`), which must be coordinated with the adapter lifecycle (`Initialize` → `Attach` → ready).
- All WebView2 API calls must happen on the UI thread.

## Goals / Non-Goals

**Goals:**
- Implement `WindowsWebViewAdapter` backed by WebView2 covering all `IWebViewAdapter` members.
- Implement optional facets: `ICookieAdapter`, `INativeWebViewHandleProvider`, `IWebViewAdapterOptions`.
- Map `CoreWebView2WebErrorStatus` to the error categorization hierarchy (`WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`).
- Support `NavigateToStringAsync` with `baseUrl` parameter.
- Register via `ModuleInitializer` following the macOS pattern.
- Provide Windows IT smoke tests covering the same scope as macOS IT.

**Non-Goals:**
- Multi-profile or custom `UserDataFolder` management — use default environment for M0.
- Custom scheme handlers or `WebResourceRequested` interception beyond the `baseUrl` implementation detail.
- Full `window.open` lifecycle management — `NewWindowRequested` event propagation only.
- GPU/rendering customization or offscreen rendering.
- Supporting older Windows versions that lack WebView2 Runtime.

## Decisions

### 1) WebView2 SDK integration approach

**Decision:** Use the `Microsoft.Web.WebView2` NuGet package (managed SDK) directly. No native code or P/Invoke required.

**Rationale:**
- The WebView2 managed SDK provides a complete .NET API surface including `CoreWebView2Environment`, `CoreWebView2Controller`, and `CoreWebView2`.
- Unlike WKWebView (which required a custom ObjC++ shim), WebView2 has first-class .NET support.
- MIT-licensed and actively maintained by Microsoft.

**Alternatives considered:**
- Use WebView2 COM interop directly: rejected as unnecessarily low-level when the managed SDK handles marshaling.
- Use WinRT/WinUI WebView2 control: rejected because our adapter provides the hosting control; we need the `CoreWebView2Controller` level for parent-handle-based hosting.

### 2) Async initialization lifecycle

**Decision:** Perform WebView2 environment and controller creation in the `Attach` method:
1. `Initialize(host)` → store host reference, validate state.
2. `Attach(parentHandle)` → create `CoreWebView2Environment`, then `CoreWebView2Controller` parented to the given HWND. Queue any navigation requests received between `Initialize` and readiness.
3. `Detach()` → close and dispose `CoreWebView2Controller`.

**Rationale:**
- WebView2 requires a parent HWND to create the controller. This maps naturally to `Attach(parentHandle)`.
- The adapter contract allows navigation APIs to be called after `Initialize` but before `Attach` is complete. Queuing requests ensures correct ordering.

**Alternatives considered:**
- Create controller eagerly with a hidden HWND: rejected because it wastes resources and deviates from the adapter lifecycle contract.
- Fail navigation calls before attach completes: rejected because it would differ from macOS behavior where the adapter buffers.

### 3) Navigation interception via NavigationStarting event

**Decision:** Subscribe to `CoreWebView2.NavigationStarting` and gate every main-frame navigation by calling `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`. Use `e.NavigationId` (WebView2's built-in uint64) as the `CorrelationId` source.

For API-initiated navigations (`NavigateAsync`, `NavigateToStringAsync`), skip the host callback (the runtime already raised `NavigationStarted` before calling the adapter).

**Rationale:**
- `NavigationStarting` fires before every navigation including redirects, matching the interception semantics required by the contract.
- WebView2's `NavigationId` is stable across redirects within the same navigation chain — it can be converted to a deterministic `CorrelationId` (e.g., hashed or mapped to a `Guid`).

**Alternatives considered:**
- Use only `NavigationCompleted` and post-hoc correlation: rejected because it doesn't enable cancel/deny before navigation proceeds.

### 4) CorrelationId strategy for redirects

**Decision:** Maintain a `Dictionary<ulong, Guid>` mapping WebView2's `NavigationId` (uint64) to our `CorrelationId` (Guid). When a `NavigationStarting` event fires:
- If the WebView2 `NavigationId` is already in the map → reuse the existing `CorrelationId` (redirect continuation).
- If new → generate a fresh `CorrelationId` and add the mapping.
- Clear the mapping entry on `NavigationCompleted` for that `NavigationId`.

**Rationale:**
- WebView2 already provides a stable `NavigationId` across redirect chains, simplifying correlation compared to the macOS adapter which had to track chain state manually.
- Direct mapping avoids any heuristics or timing dependencies.

**Alternatives considered:**
- Use WebView2's `NavigationId` directly as the CorrelationId (cast to Guid): rejected because the contract specifies Guid and a uint64-to-Guid cast is semantically unclear.

### 5) Error categorization mapping

**Decision:** Map `CoreWebView2WebErrorStatus` enum values to exception types:

| WebView2 WebErrorStatus | Exception Type |
|--------------------------|----------------|
| `Timeout` | `WebViewTimeoutException` |
| `ConnectionAborted`, `ConnectionReset`, `Disconnected`, `CannotConnect`, `HostNameNotResolved`, `ConnectionRefused` | `WebViewNetworkException` |
| `CertificateCommonNameIsIncorrect`, `CertificateExpired`, `ClientCertificateContainsErrors`, `CertificateRevoked`, `CertificateIsInvalid` | `WebViewSslException` |
| All other non-success statuses | `WebViewNavigationException` (base) |

**Rationale:**
- `CoreWebView2WebErrorStatus` provides granular error categories that map cleanly to our hierarchy.
- The mapping covers the same semantic categories as the macOS `NSURLError` mapping.

### 6) NavigateToStringAsync with baseUrl

**Decision:** When `baseUrl` is non-null, use a WebView2 `WebResourceRequested` filter + interception pattern:
1. Register a temporary `WebResourceRequested` filter for the exact `baseUrl` URI.
2. In the handler, respond with the HTML content as a `CoreWebView2WebResourceResponse` (content-type `text/html`).
3. Call `CoreWebView2.Navigate(baseUrl.ToString())` to trigger the navigation.
4. Remove the filter after the response is served.

When `baseUrl` is null, use `CoreWebView2.NavigateToString(html)` directly.

**Rationale:**
- WebView2's `NavigateToString` does not accept a `baseUrl` parameter (unlike WKWebView's `loadHTMLString:baseURL:`).
- The intercept approach gives the loaded HTML the correct origin matching `baseUrl`, enabling proper relative URL resolution and cookie/storage scoping — identical semantics to the WKWebView behavior.

**Alternatives considered:**
- Inject a `<base href="...">` tag into the HTML: rejected because it only affects relative URL resolution, not the origin (cookies, local storage, CORS would not match).
- Use a temporary file and `file://` navigation: rejected as insecure and permission-complex.

### 7) Cookie management via CoreWebView2CookieManager

**Decision:** Implement `ICookieAdapter` using `CoreWebView2.CookieManager`:
- `GetCookiesAsync(uri)` → `CookieManager.GetCookiesAsync(uri.ToString())`, map `CoreWebView2Cookie` → `WebViewCookie`
- `SetCookieAsync(cookie)` → `CookieManager.CreateCookie(...)` + `CookieManager.AddOrUpdateCookie(...)`
- `DeleteCookieAsync(cookie)` → `CookieManager.DeleteCookie(...)` (match by name/domain/path)
- `ClearAllCookiesAsync()` → `CookieManager.DeleteAllCookies()`

**Rationale:**
- WebView2 provides a first-class `CookieManager` API that maps 1:1 to our `ICookieAdapter` surface.
- All operations are synchronous-style (except `GetCookiesAsync`) but thread-safe when called from the UI thread.

### 8) WebMessage bridge (receive path)

**Decision:** Use `CoreWebView2.WebMessageReceived` event for receiving messages from web content. The adapter wires the channel-specific script injection at `Attach` time via `CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(...)` to establish `window.chrome.webview.postMessage` routing to the adapter's channel.

**Rationale:**
- WebView2 has built-in `postMessage` support via `window.chrome.webview.postMessage` — no custom bridge injection needed for the receive path.
- The runtime handles policy gating; the adapter just forwards the raw message.

### 9) Native handle provider

**Decision:** Implement `INativeWebViewHandleProvider` returning the `CoreWebView2Controller` handle wrapped in a `PlatformHandle("WebView2")`.

**Rationale:**
- `CoreWebView2Controller` is the primary hosting object that consumers need for advanced scenarios (resizing, visibility, focus).
- Descriptor `"WebView2"` follows the same pattern as macOS's `"WKWebView"`.

### 10) Module initializer registration

**Decision:** Add `WindowsAdapterModule` with `[ModuleInitializer]` that calls `WebViewAdapterRegistry.Register(...)` when `OperatingSystem.IsWindows()`, following the exact pattern of `MacOSAdapterModule`.

**Rationale:**
- Consistent with the existing adapter-as-plugin registration pattern.
- No changes needed to the registry infrastructure.

## Risks / Trade-offs

- **[WebView2 Runtime availability]** Not all Windows machines have WebView2 Runtime installed (though it ships with Windows 10 21H2+ and all Windows 11). → Mitigation: detect runtime availability in `Attach` and throw a clear `InvalidOperationException` with installation instructions if missing.
- **[Async initialization latency]** `CoreWebView2Environment.CreateAsync` can take 100-500ms on first launch (creates browser process). → Mitigation: queue operations during initialization; document startup latency expectations.
- **[baseUrl intercept complexity]** The `WebResourceRequested` intercept for baseUrl adds a one-time handler per `NavigateToStringAsync(html, baseUrl)` call. → Mitigation: ensure filter registration/removal is deterministic and test for re-entrant calls.
- **[Thread affinity]** All WebView2 APIs require UI-thread access. → Mitigation: adapter methods validate thread affinity; runtime already marshals async APIs to UI thread via dispatcher.
- **[DeleteAllCookies is synchronous and fire-and-forget in WebView2]** No completion callback. → Mitigation: wrap in Task.CompletedTask after invocation; document eventual consistency similar to WKHTTPCookieStore.
- **[NavigationStarting event does not distinguish initial request from redirect]** WebView2 reuses the same `NavigationId` for redirects, which simplifies correlation but means we cannot know from the event alone whether this is a redirect. → Mitigation: not needed — the contract only requires stable CorrelationId, not redirect detection.

## Open Questions

- Should the adapter support WebView2's `BrowserProcessExited` event for crash recovery, or is this deferred to a later milestone?
- Should `IWebViewAdapterOptions.ApplyEnvironmentOptions` configure `CoreWebView2EnvironmentOptions` (e.g., `--disable-gpu`, additional browser arguments), or keep M0 limited to DevTools + UserAgent?
- For IT smoke tests: can we share the existing loopback HTTP test server from the macOS IT suite, or does Windows need a separate test harness?
