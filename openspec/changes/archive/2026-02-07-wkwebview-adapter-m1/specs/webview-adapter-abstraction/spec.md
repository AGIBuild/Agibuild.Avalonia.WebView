## ADDED Requirements

### Requirement: NavigateToStringAsync adapter overload with baseUrl
The `IWebViewAdapter` interface SHALL define an additional overload:
- `Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)`

The existing two-parameter `NavigateToStringAsync(Guid navigationId, string html)` SHALL delegate to the new overload with `baseUrl: null`.
Adapters SHALL pass `baseUrl` to the native WebView's HTML loading API when non-null.

#### Scenario: Adapter receives baseUrl for HTML navigation
- **WHEN** the runtime calls `NavigateToStringAsync(navigationId, html, baseUrl)` with a non-null `baseUrl`
- **THEN** the adapter passes the `baseUrl` to the native HTML loading API

#### Scenario: Adapter receives null baseUrl by default
- **WHEN** the runtime calls the two-parameter overload
- **THEN** the adapter receives `baseUrl: null`

### Requirement: Optional ICookieAdapter facet
The adapter abstractions SHALL define an `ICookieAdapter` interface that adapters MAY implement alongside `IWebViewAdapter`:
- `Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri)`
- `Task SetCookieAsync(WebViewCookie cookie)`
- `Task DeleteCookieAsync(WebViewCookie cookie)`
- `Task ClearAllCookiesAsync()`

The runtime SHALL detect `ICookieAdapter` support via type check on the adapter instance at initialization time.
Adapters that do not implement `ICookieAdapter` SHALL NOT be required to provide cookie stubs.

#### Scenario: Adapter implementing ICookieAdapter enables cookie manager
- **WHEN** an adapter implements both `IWebViewAdapter` and `ICookieAdapter`
- **THEN** the runtime detects cookie support and `TryGetCookieManager()` returns a non-null instance

#### Scenario: Adapter without ICookieAdapter returns null cookie manager
- **WHEN** an adapter implements only `IWebViewAdapter`
- **THEN** `TryGetCookieManager()` returns `null`

### Requirement: INativeWebViewHandleProvider is implementable by adapters
Platform adapters MAY implement `INativeWebViewHandleProvider` to expose the underlying native WebView handle.
The `HandleDescriptor` property of the returned `IPlatformHandle` SHALL identify the native type (e.g., `"WKWebView"`, `"WebView2"`).

#### Scenario: macOS adapter exposes WKWebView handle
- **WHEN** the macOS adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "WKWebView"`

## MODIFIED Requirements

### Requirement: IWebViewAdapter contract surface
The `IWebViewAdapter` interface SHALL define:
- lifecycle: `void Initialize(IWebViewAdapterHost host)`, `void Attach(IPlatformHandle parentHandle)`, `void Detach()`
- navigation: `Task NavigateAsync(Guid navigationId, Uri uri)`, `Task NavigateToStringAsync(Guid navigationId, string html)`, `Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)`
- scripting: `Task<string?> InvokeScriptAsync(string script)`
- commands: `bool GoBack(Guid navigationId)`, `bool GoForward(Guid navigationId)`, `bool Refresh(Guid navigationId)`, `bool Stop()`
- state: `bool CanGoBack { get; }`, `bool CanGoForward { get; }`
- events: `NavigationCompleted`, `NewWindowRequested`, `WebMessageReceived`, `WebResourceRequested`, `EnvironmentRequested`

#### Scenario: IWebViewAdapter members are available
- **WHEN** a consumer reflects on `IWebViewAdapter`
- **THEN** all listed members are present with the specified signatures
