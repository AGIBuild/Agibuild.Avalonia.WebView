## Context

M0 delivered a functional macOS WKWebView adapter with core navigation interception, correlation, scripting, and WebMessage receive path. The adapter and runtime are wired end-to-end and validated by contract tests (CT) plus an integration smoke suite (IT).

M1 addresses the remaining gaps visible in the compatibility matrix's "Extended" tier and solidifies the adapter's production readiness:

| Gap | Current state |
|-----|--------------|
| Cookie management | `ICookieManager` is an empty placeholder; `TryGetCookieManager()` returns `null` |
| New-window fallback | Runtime propagates `NewWindowRequested` but does **not** navigate in-view when `Handled == false` |
| Native handle | `INativeWebViewHandleProvider` interface exists; no adapter implements it |
| Error granularity | All non-canceled failures map to a bare `Exception`; no distinction between network, SSL, timeout |
| `NavigateToStringAsync` baseUrl | Native shim accepts `baseUrl` but adapter always passes `null` |

Constraints:
- All contract changes must be backwards-compatible (additive only).
- The `ICookieManager` API must be platform-agnostic; macOS implementation via `WKHTTPCookieStore` is a first adapter, other platforms may follow with different backing stores.
- New capabilities remain `[Experimental]` until validated on at least two platform adapters.

## Goals / Non-Goals

**Goals:**
- Define a concrete `ICookieManager` API (get/set/delete/clear) and implement it on macOS via `WKHTTPCookieStore`.
- Wire `WebViewCore` to create and return a platform cookie manager from `TryGetCookieManager()` when the adapter supports it.
- Implement the `NewWindowRequested` unhandled fallback: when `Handled == false` and `Uri` is non-null, the runtime navigates to the URI in the current view.
- Implement `INativeWebViewHandleProvider` in the macOS adapter, exposing the `WKWebView` pointer as an `IPlatformHandle`.
- Introduce `WebViewNavigationException` subclasses (`WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`) and map additional `NSURLError` codes in the native shim.
- Add a `NavigateToStringAsync(string html, Uri? baseUrl)` overload across the contract → adapter → runtime stack, passing `baseUrl` to the native shim.
- Extend CT and IT to cover the new capabilities.

**Non-Goals:**
- Implement `ICookieManager` on non-macOS adapters (Windows, Android, Gtk) — left for future milestones.
- Implement `ICommandManager` — remains a placeholder.
- Handle `window.open` with full multi-window lifecycle — `NewWindowRequested` only handles the fallback policy.
- Implement `WebResourceRequested` or `EnvironmentRequested` beyond their current placeholders.

## Decisions

### 1) ICookieManager API shape

**Decision:** Extend `ICookieManager` with async CRUD operations using a `WebViewCookie` record:

```csharp
public interface ICookieManager
{
    Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri);
    Task SetCookieAsync(WebViewCookie cookie);
    Task DeleteCookieAsync(WebViewCookie cookie);
    Task ClearAllCookiesAsync();
}

public sealed record WebViewCookie(
    string Name,
    string Value,
    string Domain,
    string Path,
    DateTimeOffset? Expires,
    bool IsSecure,
    bool IsHttpOnly);
```

**Rationale:**
- Matches the minimal surface needed for session management and OAuth flows.
- `WebViewCookie` is a value-type record — immutable, no platform dependency.
- Async-only aligns with `WKHTTPCookieStore` (all operations are completion-handler-based) and WebView2's `CookieManager`.

**Alternatives considered:**
- Sync API: rejected because `WKHTTPCookieStore` is inherently async; sync wrappers would deadlock on the main thread.
- Exposing `NSHTTPCookie` / `HttpCookie` directly: rejected for cross-platform abstraction purity.

### 2) Cookie adapter facet pattern

**Decision:** Introduce an optional `ICookieAdapter` interface that adapters may implement alongside `IWebViewAdapter`. `WebViewCore` detects this via a type check at initialization and returns a wrapping `ICookieManager` from `TryGetCookieManager()` if available.

```csharp
internal interface ICookieAdapter
{
    Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri);
    Task SetCookieAsync(WebViewCookie cookie);
    Task DeleteCookieAsync(WebViewCookie cookie);
    Task ClearAllCookiesAsync();
}
```

**Rationale:**
- Keeps `IWebViewAdapter` lean — cookie support is optional and not all platforms may implement it initially.
- Avoids breaking existing adapters that don't support cookies (they simply don't implement `ICookieAdapter`).
- The runtime wrapping layer can add dispatcher marshaling and lifecycle guards.

**Alternatives considered:**
- Add cookie methods directly to `IWebViewAdapter`: rejected because it forces all adapters to implement stubs.
- Separate registration via DI: rejected as over-engineered for an adapter-internal concern.

### 3) macOS WKHTTPCookieStore integration

**Decision:** In `MacOSWebViewAdapter`, implement `ICookieAdapter` by:
- Accessing the cookie store via `WKWebsiteDataStore.defaultDataStore.httpCookieStore` in the native shim.
- Exposing C ABI functions: `CookiesGet(url, callback)`, `CookieSet(fields, callback)`, `CookieDelete(fields, callback)`, `CookiesClearAll(callback)`.
- Marshaling `NSHTTPCookie` ↔ `WebViewCookie` via JSON or structured P/Invoke parameters.

**Rationale:**
- `WKHTTPCookieStore` is the only documented API for programmatic cookie access in WKWebView.
- C ABI + callback pattern is consistent with the existing M0 native shim approach.

### 4) NewWindowRequested unhandled fallback

**Decision:** After raising `NewWindowRequested` on the UI thread, check `e.Handled`. If `false` and `e.Uri` is non-null, call `NavigateAsync(e.Uri)` on the current `WebViewCore` instance.

**Rationale:**
- This matches the documented contract in `NewWindowRequestedEventArgs.Handled`: _"When unhandled, the WebView control will navigate to the URI in the current view instead of opening a new window."_
- The runtime already has the method; it just needs to call it conditionally.

**Alternatives considered:**
- Ignore unhandled events (current behavior): rejected because it violates the documented contract.
- Open in system browser: rejected as it deviates from the in-view contract.

### 5) INativeWebViewHandleProvider implementation

**Decision:** Have `MacOSWebViewAdapter` implement `INativeWebViewHandleProvider`. The native shim exposes a `GetWebViewHandle()` function returning the `WKWebView*` pointer. The adapter wraps it in a `PlatformHandle("WKWebView")`.

**Rationale:**
- Simple pass-through; the native shim already retains the `WKWebView` reference.
- `HandleDescriptor = "WKWebView"` lets consumers identify the handle type.

### 6) Navigation error hierarchy

**Decision:** Introduce specific exception subclasses under the existing `WebViewNavigationException`:

```
WebViewNavigationException (existing)
├── WebViewNetworkException     (connectivity, DNS, unreachable)
├── WebViewSslException         (certificate, trust chain)
└── WebViewTimeoutException     (request/resource timeout)
```

Map additional `NSURLError` codes in the native shim:
- `NSURLErrorTimedOut` (-1001) → status code 3 (Timeout)
- `NSURLErrorCannotFindHost` (-1003), `NSURLErrorCannotConnectToHost` (-1004), `NSURLErrorNetworkConnectionLost` (-1005), `NSURLErrorNotConnectedToInternet` (-1009) → status code 4 (Network)
- `NSURLErrorServerCertificateHasBadDate` (-1201), `NSURLErrorServerCertificateUntrusted` (-1202), `NSURLErrorServerCertificateHasUnknownRoot` (-1203), `NSURLErrorServerCertificateNotYetValid` (-1204) → status code 5 (Ssl)

Extend the native shim status mapping from 3 values (0=Success, 1=Failure, 2=Canceled) to 6 values (+3=Timeout, +4=Network, +5=Ssl).

**Rationale:**
- Consumers need error categorization for retry logic, UI messaging, and diagnostics.
- Subclassing preserves backwards compatibility — existing `catch (WebViewNavigationException)` still works.
- Aligning with `NSURLError` codes is straightforward and well-documented.

**Alternatives considered:**
- Add an enum property to `WebViewNavigationException` instead of subclasses: viable but less idiomatic for .NET exception handling patterns (`catch` by type).
- Map every `NSURLError` code: rejected as over-scoped; cover the most common categories first.

### 7) NavigateToStringAsync baseUrl overload

**Decision:** Add an overload `NavigateToStringAsync(string html, Uri? baseUrl)` to `IWebView`, `IWebViewAdapter`, and `WebViewCore`. The existing single-parameter overload delegates to the new one with `baseUrl: null`.

**Rationale:**
- The native shim already supports `baseUrl` (it's passed to `[WKWebView loadHTMLString:baseURL:]`).
- `baseUrl` enables relative resource resolution in loaded HTML — important for offline content and hybrid apps.
- Adding an overload is non-breaking.

**Alternatives considered:**
- Use a separate `NavigateToStringWithBaseUrlAsync` method: rejected as inconsistent with .NET overload conventions.
- Require callers to embed `<base href>` in HTML: rejected as error-prone and not equivalent to the native `baseURL` parameter.

## Risks / Trade-offs

- **[WKHTTPCookieStore timing]** Cookie operations on `WKHTTPCookieStore` are async and may not reflect immediately in the WebView. → Mitigation: document eventual consistency; test with delays in IT.
- **[Error code mapping incompleteness]** Only the most common `NSURLError` codes are mapped; rare errors fall back to generic `Failure`. → Mitigation: log unmapped codes; extendable mapping table in native shim.
- **[NewWindowRequested fallback navigation re-triggers interception]** Navigating in-view from the fallback will trigger `NavigationStarted` again. → Mitigation: this is expected and correct per the contract; document the re-entrant behavior.
- **[ICookieManager API evolving]** The interface remains `[Experimental]` — it may change before stabilization. → Mitigation: keep the `AGWV001` diagnostic ID; consumers opt-in explicitly.
- **[baseUrl semantic differences across platforms]** `baseUrl` behavior may differ between WKWebView and WebView2. → Mitigation: document platform differences in compatibility matrix; restrict M1 to macOS validation.

## Open Questions

- Should `ICookieManager` support cookie change observation (like `WKHTTPCookieStore.addObserver`)? Deferred unless a clear use case emerges.
- Should `ClearAllCookiesAsync` clear all website data or only cookies? Current proposal: cookies only via `WKHTTPCookieStore` API.
