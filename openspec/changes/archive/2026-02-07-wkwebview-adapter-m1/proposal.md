## Why

M0 delivered the macOS WKWebView adapter's core navigation, interception, scripting, and WebMessage capabilities. However, several contract-level features remain unimplemented or only partially wired: cookie management (`ICookieManager`) is a placeholder, the `NewWindowRequested` event is raised by the adapter but not connected through the runtime, native handle access is missing, and navigation errors lack granularity. M1 closes these gaps to bring the macOS adapter to full baseline + extended parity as defined by the compatibility matrix.

## What Changes

- **ICookieManager API & macOS implementation**: Define cookie CRUD operations on the currently-empty `ICookieManager` interface and implement via `WKHTTPCookieStore`. Wire `WebViewCore.TryGetCookieManager()` to return a live instance when supported.
- **NewWindowRequested runtime wiring**: The macOS adapter already detects `targetFrame == nil` and raises `NewWindowRequested`, but the runtime does not propagate it to `IWebView.NewWindowRequested`. Wire the adapter event through `WebViewCore` and respect the `Handled` flag (navigate in-view when unhandled).
- **INativeWebViewHandleProvider for macOS**: Expose the underlying `WKWebView` pointer via the existing `INativeWebViewHandleProvider` interface.
- **Navigation error categorization**: Introduce a `WebViewNavigationException` hierarchy (network, SSL/TLS, timeout) replacing the generic `Exception` in `NavigationCompletedEventArgs.Error`, and map additional `NSURLError` codes in the native shim.
- **NavigateToStringAsync baseUrl support**: The native shim already accepts a `baseUrl` parameter but the adapter always passes `null`. Expose a `baseUrl` parameter through the adapter abstraction and runtime API.
- **Contract tests & IT extensions**: Add unit tests for cookie lifecycle, new-window propagation, error categorization, and extend the macOS integration smoke suite accordingly.

## Capabilities

### New Capabilities
- `webview-cookie-management`: ICookieManager API design and macOS WKHTTPCookieStore implementation covering get/set/delete/clear operations.

### Modified Capabilities
- `webview-core-contracts`: Add `NavigateToStringAsync(string html, Uri? baseUrl)` overload; define `WebViewNavigationException` hierarchy; evolve `ICookieManager` from placeholder to concrete API.
- `webview-adapter-abstraction`: Add `NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)` overload; add optional `ICookieAdapter` facet; wire `NewWindowRequested` through adapter host callback pattern.
- `webview-contract-semantics-v1`: Add semantics for cookie operations, new-window propagation, error categorization, and baseUrl behavior.
- `webview-compatibility-matrix`: Record macOS M1 acceptance criteria and updated capability status.
- `webview-testing-harness`: Extend `MockWebViewAdapter` with cookie and new-window simulation helpers.

## Impact

- **Core**: `WebViewContracts.cs` — `ICookieManager` gains methods; new `WebViewNavigationException` types; `IWebView` gains `NavigateToStringAsync` overload.
- **Adapter abstraction**: `IWebViewAdapter` gains `NavigateToStringAsync` overload and optional `ICookieAdapter`; `IWebViewAdapterHost` gains `OnNewWindowRequested` callback (or adapter event wiring).
- **Runtime**: `WebViewCore` — wire cookie manager, new-window event, error mapping, baseUrl pass-through.
- **macOS adapter**: `MacOSWebViewAdapter` — implement `ICookieAdapter` via `WKHTTPCookieStore`; implement `INativeWebViewHandleProvider`; extend error mapping in native shim for additional `NSURLError` codes.
- **Tests**: New contract tests for cookie CRUD, new-window semantics, error types; extended mock adapter; macOS IT smoke additions.
- **No breaking changes**: All additions are additive; existing M0 behavior is preserved.
