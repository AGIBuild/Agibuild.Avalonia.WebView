## Purpose
Define platform-agnostic adapter abstraction contracts and lifecycle guarantees.

## Requirements

### Requirement: Adapter abstractions assembly
The system SHALL provide an assembly named `Agibuild.Fulora.Adapters.Abstractions` targeting `net10.0`.
This assembly SHALL reference `Agibuild.Fulora.Core` and SHALL NOT reference any platform-specific adapter projects.

#### Scenario: Abstractions are platform-free
- **WHEN** a project references `Agibuild.Fulora.Adapters.Abstractions`
- **THEN** it builds without any platform-specific adapter dependencies

### Requirement: IWebViewAdapter contract surface
The `IWebViewAdapter` interface SHALL define:
- lifecycle: `void Initialize(IWebViewAdapterHost host)`, `void Attach(INativeHandle parentHandle)`, `void Detach()`
- navigation: `Task NavigateAsync(Guid navigationId, Uri uri)`, `Task NavigateToStringAsync(Guid navigationId, string html)`, `Task NavigateToStringAsync(Guid navigationId, string html, Uri? baseUrl)`
- scripting: `Task<string?> InvokeScriptAsync(string script)`
- commands: `bool GoBack(Guid navigationId)`, `bool GoForward(Guid navigationId)`, `bool Refresh(Guid navigationId)`, `bool Stop()`
- state: `bool CanGoBack { get; }`, `bool CanGoForward { get; }`
- events: `NavigationCompleted`, `NewWindowRequested`, `WebMessageReceived`, `WebResourceRequested`, `EnvironmentRequested`

#### Scenario: IWebViewAdapter members are available
- **WHEN** a consumer reflects on `IWebViewAdapter`
- **THEN** all listed members are present with the specified signatures

### Requirement: Adapter lifecycle sequencing
The system SHALL enforce the following lifecycle ordering for each adapter instance:
- `Initialize(host)` is called exactly once.
- `Attach(parentHandle)` is called only after `Initialize(host)`.
- `Detach()` is called at most once and only after `Attach(parentHandle)`.
- After `Detach()`, the adapter SHALL NOT raise any further events.

#### Scenario: Attach happens after initialize
- **WHEN** a host attaches a WebView to the visual tree
- **THEN** it calls `Initialize` before `Attach`

#### Scenario: No events after detach
- **WHEN** `Detach()` is called
- **THEN** subsequent adapter-originated events are not emitted

### Requirement: Adapter reports native-initiated navigations via host callback
To enable full control of navigation (including navigations initiated by the web content), platform adapters SHALL consult the host via `IWebViewAdapterHost` before allowing a native-initiated navigation to proceed.

The host callback SHALL provide an allow/deny decision suitable for platform navigation interception. The decision MAY be returned asynchronously, but it MUST be deterministic and MUST NOT rely on timing sleeps.

#### Scenario: Native-initiated navigation can be denied
- **WHEN** a web content action triggers a native navigation (e.g. link click, redirect)
- **THEN** the adapter calls the host callback and honors an allow/deny decision deterministically

### Requirement: Redirect correlation via CorrelationId
For main-frame native-initiated navigations, the adapter SHALL:
- call `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)` before each navigation step that can be intercepted (including redirects)
- provide a non-empty `CorrelationId`
- reuse the same `CorrelationId` across all steps within a single logical navigation chain (including redirects) until completion
- NOT reuse a `CorrelationId` concurrently for a different main-frame navigation chain

#### Scenario: Redirects reuse CorrelationId
- **WHEN** a native-initiated navigation produces multiple navigation-start callbacks due to redirects
- **THEN** the adapter uses the same `CorrelationId` for each step in the redirect chain

### Requirement: Adapter uses host-issued NavigationId for completions
For any navigation that the adapter allows to proceed, the adapter SHALL:
- use the `NavigationId` returned by the host decision when raising `NavigationCompleted`
- NOT raise `NavigationCompleted` for a `NavigationId` that was not issued by the host (native-initiated) or passed into the adapter API (API-initiated)

#### Scenario: Completion is correlated to host-issued NavigationId
- **WHEN** the host allows a native-initiated navigation and returns a `NavigationId`
- **THEN** the adapter raises `NavigationCompleted` using that same `NavigationId`

### Requirement: Event parity between adapter and core
The adapter event args SHALL use the Core event args types (`NavigationCompletedEventArgs`, `NewWindowRequestedEventArgs`, `WebMessageReceivedEventArgs`, `WebResourceRequestedEventArgs`, `EnvironmentRequestedEventArgs`).

#### Scenario: Adapter events use core event args
- **WHEN** a consumer inspects event signatures on `IWebViewAdapter`
- **THEN** each event uses the corresponding Core event args type

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

### Requirement: Optional ICustomSchemeAdapter facet
The adapter abstractions SHALL define an `ICustomSchemeAdapter` interface that adapters MAY implement alongside `IWebViewAdapter`:
- `void RegisterCustomSchemes(IReadOnlyList<CustomSchemeRegistration> schemes)`

The runtime SHALL detect `ICustomSchemeAdapter` support via type check at initialization.
When registered custom schemes are requested by the WebView, the adapter SHALL raise `WebResourceRequested` with the request details.

#### Scenario: Adapter implementing ICustomSchemeAdapter enables custom scheme interception
- **WHEN** an adapter implements both `IWebViewAdapter` and `ICustomSchemeAdapter`
- **THEN** the runtime calls `RegisterCustomSchemes` with the configured schemes
- **AND** requests to those schemes raise `WebResourceRequested`

### Requirement: Optional IDownloadAdapter facet
The adapter abstractions SHALL define an `IDownloadAdapter` interface that adapters MAY implement alongside `IWebViewAdapter`:
- `event EventHandler<DownloadRequestedEventArgs>? DownloadRequested`

The runtime SHALL detect `IDownloadAdapter` support via type check and subscribe to the event.

#### Scenario: Adapter implementing IDownloadAdapter enables download events
- **WHEN** an adapter implements both `IWebViewAdapter` and `IDownloadAdapter`
- **THEN** download events are propagated to consumers via `IWebView.DownloadRequested`

### Requirement: Optional IPermissionAdapter facet
The adapter abstractions SHALL define an `IPermissionAdapter` interface that adapters MAY implement alongside `IWebViewAdapter`:
- `event EventHandler<PermissionRequestedEventArgs>? PermissionRequested`

The runtime SHALL detect `IPermissionAdapter` support via type check and subscribe to the event.

#### Scenario: Adapter implementing IPermissionAdapter enables permission events
- **WHEN** an adapter implements both `IWebViewAdapter` and `IPermissionAdapter`
- **THEN** permission events are propagated to consumers via `IWebView.PermissionRequested`

### Requirement: INativeWebViewHandleProvider is implementable by adapters
Platform adapters SHALL expose the underlying native WebView handle through `INativeWebViewHandleProvider` when native-handle access is supported.
The `HandleDescriptor` property of the returned `INativeHandle` SHALL identify the native type (e.g., `"WKWebView"`, `"WebView2"`).
The returned `INativeHandle` SHALL also implement the appropriate typed platform handle interface from Core:
- Windows adapters: `IWindowsWebView2PlatformHandle`
- macOS/iOS adapters: `IAppleWKWebViewPlatformHandle`
- GTK adapters: `IGtkWebViewPlatformHandle`
- Android adapters: `IAndroidWebViewPlatformHandle`

#### Scenario: macOS adapter exposes WKWebView handle
- **WHEN** the macOS adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "WKWebView"`
- **AND** the handle implements `IAppleWKWebViewPlatformHandle`

#### Scenario: Windows adapter exposes WebView2 handle
- **WHEN** the Windows adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "WebView2"`
- **AND** the handle implements `IWindowsWebView2PlatformHandle`

#### Scenario: Android adapter exposes Android WebView handle
- **WHEN** the Android adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "AndroidWebView"`
- **AND** the handle implements `IAndroidWebViewPlatformHandle`

#### Scenario: GTK adapter exposes WebKitGTK handle
- **WHEN** the GTK adapter implements `INativeWebViewHandleProvider`
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "WebKitGTK"`
- **AND** the handle implements `IGtkWebViewPlatformHandle`
