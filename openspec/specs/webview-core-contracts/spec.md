## Purpose
Define the platform-agnostic Core contract surface, required types, and compatibility expectations for WebView consumers and adapters.

## Requirements

### Requirement: Core contracts assembly
The system SHALL provide a public contracts assembly named `Agibuild.Fulora.Core` with root namespace `Agibuild.Fulora`.
The assembly SHALL target `net10.0` and SHALL NOT reference any platform adapter projects.

#### Scenario: Core is platform-agnostic
- **WHEN** a project references `Agibuild.Fulora.Core` only
- **THEN** it builds without any platform-specific adapter dependencies

### Requirement: IWebView contract surface
The `IWebView` interface SHALL define:
- properties: `Uri Source { get; set; }`, `bool CanGoBack { get; }`, `bool CanGoForward { get; }`, `Guid ChannelId { get; }`
- methods: `Task NavigateAsync(Uri uri)`, `Task NavigateToStringAsync(string html)`, `Task NavigateToStringAsync(string html, Uri? baseUrl)`, `Task<string?> InvokeScriptAsync(string script)`
- commands: `bool GoBack()`, `bool GoForward()`, `bool Refresh()`, `bool Stop()`
- accessors: `ICookieManager? TryGetCookieManager()`, `ICommandManager? TryGetCommandManager()`
- events: `NavigationStarted`, `NavigationCompleted`, `NewWindowRequested`, `WebMessageReceived`, `WebResourceRequested`, `EnvironmentRequested`, `DownloadRequested`, `PermissionRequested`, `AdapterCreated`, `AdapterDestroyed`

#### Scenario: IWebView members are available
- **WHEN** a consumer reflects on `IWebView`
- **THEN** all listed members are present with the specified signatures

### Requirement: IWebDialog contract surface
The `IWebDialog` interface SHALL inherit from `IWebView` and define:
- properties: `string? Title { get; set; }`, `bool CanUserResize { get; set; }`
- methods: `void Show()`, `bool Show(IPlatformHandle owner)`, `void Close()`, `bool Resize(int width, int height)`, `bool Move(int x, int y)`
- events: `Closing`

#### Scenario: IWebDialog members are available
- **WHEN** a consumer reflects on `IWebDialog`
- **THEN** all listed members are present with the specified signatures

### Requirement: Authentication broker contracts
The `IWebAuthBroker` interface SHALL define:
- `Task<WebAuthResult> AuthenticateAsync(ITopLevelWindow owner, AuthOptions options)`

#### Scenario: IWebAuthBroker is callable
- **WHEN** a consumer references `IWebAuthBroker.AuthenticateAsync`
- **THEN** it accepts `ITopLevelWindow` and `AuthOptions` and returns `Task<WebAuthResult>`

### Requirement: Environment options and native handles
The Core assembly SHALL define:
- `IWebViewEnvironmentOptions` with `bool EnableDevTools { get; set; }`, `IReadOnlyList<CustomSchemeRegistration>? CustomSchemes { get; set; }`
- `INativeWebViewHandleProvider` with `IPlatformHandle? TryGetWebViewHandle()`
- `IWindowsWebView2PlatformHandle` extending `IPlatformHandle` with `nint CoreWebView2Handle` and `nint CoreWebView2ControllerHandle`
- `IAppleWKWebViewPlatformHandle` extending `IPlatformHandle` with `nint WKWebViewHandle`
- `IGtkWebViewPlatformHandle` extending `IPlatformHandle` with `nint WebKitWebViewHandle`
- `IAndroidWebViewPlatformHandle` extending `IPlatformHandle` with `nint AndroidWebViewHandle`

#### Scenario: Environment and handle types are resolvable
- **WHEN** a project references these types
- **THEN** it compiles without missing type errors

### Requirement: Event args types
The Core assembly SHALL define event args types:
- `NavigationStartingEventArgs` with `Guid NavigationId { get; }`, `Uri RequestUri { get; }` and `bool Cancel { get; set; }`
- `NavigationCompletedEventArgs` with `Guid NavigationId { get; }`, `Uri RequestUri { get; }`, `NavigationCompletedStatus Status { get; }`, and `Exception? Error { get; }`
- `NewWindowRequestedEventArgs`
- `WebMessageReceivedEventArgs` with `string Body { get; }`, `string Origin { get; }`, and `Guid ChannelId { get; }`
- `WebResourceRequestedEventArgs` with `Uri RequestUri { get; }`, `string Method { get; }`, `IReadOnlyDictionary<string, string>? RequestHeaders { get; }`, response properties (`Stream? ResponseBody`, `string? ResponseContentType`, `int? ResponseStatusCode`, `IDictionary<string, string>? ResponseHeaders`)
- `DownloadRequestedEventArgs` with `Uri DownloadUri { get; }`, `string? SuggestedFileName { get; }`, `string? ContentType { get; }`, `long? ContentLength { get; }`, `string? DownloadPath { get; set; }`, `bool Cancel { get; set; }`, `bool Handled { get; set; }`
- `PermissionRequestedEventArgs` with `WebViewPermissionKind PermissionKind { get; }`, `Uri? Origin { get; }`, `PermissionState State { get; set; }`
- `EnvironmentRequestedEventArgs`
- `AdapterCreatedEventArgs` with `IPlatformHandle? PlatformHandle { get; }`

#### Scenario: Event args are available
- **WHEN** a consumer references the event args types
- **THEN** all listed types are present in the Core assembly

### Requirement: Navigation, permission, and auth status enums
The Core assembly SHALL define the enums:
- `NavigationCompletedStatus` with members: `Success`, `Failure`, `Canceled`, `Superseded`
- `WebAuthStatus` with members: `Success`, `UserCancel`, `Timeout`, `Error`
- `WebViewPermissionKind` with members: `Unknown`, `Camera`, `Microphone`, `Geolocation`, `Notifications`
- `PermissionState` with members: `Default`, `Allow`, `Deny`

#### Scenario: Status enums are resolvable
- **WHEN** a consumer references `NavigationCompletedStatus` and `WebAuthStatus`
- **THEN** it compiles without missing type errors

### Requirement: WebMessage drop diagnostics types
The Core assembly SHALL define the enum `WebMessageDropReason` with members:
`OriginNotAllowed`, `ProtocolMismatch`, `ChannelMismatch`.

#### Scenario: Drop reason enum is resolvable
- **WHEN** a consumer references `WebMessageDropReason`
- **THEN** it compiles without missing type errors

### Requirement: Navigation and script exception types
The Core assembly SHALL define exception types:
- `WebViewNavigationException` used for navigation failures
- `WebViewNetworkException` for connectivity failures, inheriting from `WebViewNavigationException`
- `WebViewSslException` for TLS/certificate failures, inheriting from `WebViewNavigationException`
- `WebViewTimeoutException` for timeout failures, inheriting from `WebViewNavigationException`
- `WebViewScriptException` used for script execution failures

#### Scenario: Exception types are resolvable
- **WHEN** a consumer references the exception types
- **THEN** it compiles without missing type errors

### Requirement: Core defines IWebViewDispatcher contract
The Core assembly SHALL define an `IWebViewDispatcher` interface to support:
- UI-thread identity checks
- deterministic marshaling of work onto the UI thread for async APIs and event delivery

#### Scenario: IWebViewDispatcher is resolvable
- **WHEN** a consumer references `IWebViewDispatcher` from the Core assembly
- **THEN** it compiles without missing type errors

### Requirement: Core defines adapter host callback contract for native navigation control
The Core assembly SHALL define an adapter-host callback contract that enables platform adapters to consult the runtime before allowing any native-initiated navigation (including redirects) to proceed.

The Core assembly SHALL define:
- `IWebViewAdapterHost` exposing:
  - `Guid ChannelId { get; }`
  - `ValueTask<NativeNavigationStartingDecision> OnNativeNavigationStartingAsync(NativeNavigationStartingInfo info)`
- `NativeNavigationStartingInfo` containing, at minimum:
  - `Guid CorrelationId`
  - `Uri RequestUri`
  - `bool IsMainFrame`
- `NativeNavigationStartingDecision` containing, at minimum:
  - `bool IsAllowed`
  - `Guid NavigationId`

#### Requirement: CorrelationId is stable and non-empty
For any main-frame native-initiated navigation, `NativeNavigationStartingInfo.CorrelationId`:
- SHALL NOT be `Guid.Empty`.
- SHALL be stable across all native navigation start callbacks that belong to the same logical navigation chain, including server/client redirects before completion.
- SHALL NOT be reused concurrently for a different main-frame navigation chain within the same WebView instance.

The runtime uses `CorrelationId` to correlate redirects deterministically without relying on platform-specific redirect event shapes.

#### Scenario: Adapter host callback types are resolvable
- **WHEN** a consumer references `IWebViewAdapterHost` and the native navigation types from the Core assembly
- **THEN** it compiles without missing type errors

### Requirement: WebMessage policy contracts are defined in Core
The Core assembly SHALL define WebMessage policy contracts that enable:
- origin allowlisting
- protocol/version checks
- per-instance channel isolation
- deterministic Contract Tests (CT) without relying on platform behavior

#### Scenario: Policy contracts are resolvable
- **WHEN** a consumer references the WebMessage policy contracts
- **THEN** they compile without missing type errors

### Requirement: WebMessage envelope and decision types exist
The Core assembly SHALL define:
- a WebMessage envelope/metadata type exposing `Origin`, `ChannelId`, and `ProtocolVersion`
- a policy decision type exposing allow/deny and, when denied, a `WebMessageDropReason`

#### Scenario: Envelope and decision types are available
- **WHEN** a consumer references the WebMessage envelope and decision types
- **THEN** they compile without missing type errors

### Requirement: Drop diagnostics sink exists
The Core assembly SHALL define a testable diagnostics sink contract for dropped WebMessages.
The diagnostics sink SHALL receive, at minimum: drop reason, origin, and channel id.

#### Scenario: Diagnostics sink contract is resolvable
- **WHEN** a consumer references the diagnostics sink contract
- **THEN** it compiles without missing type errors

### Requirement: Auxiliary core types
The Core assembly SHALL define the types `ICookieManager`, `ICommandManager`, `AuthOptions`, `WebAuthResult`, and `WebViewCookie`.
`ICookieManager` SHALL define async CRUD operations: `GetCookiesAsync`, `SetCookieAsync`, `DeleteCookieAsync`, `ClearAllCookiesAsync`.

#### Scenario: Auxiliary types are resolvable
- **WHEN** a project references these types
- **THEN** it compiles without missing type errors

### Requirement: NavigateToStringAsync overload with baseUrl
The `IWebView` interface SHALL define an additional overload:
- `Task NavigateToStringAsync(string html, Uri? baseUrl)`

When `baseUrl` is non-null, relative resource references in the HTML SHALL resolve against the provided `baseUrl`.
When `baseUrl` is null, behavior SHALL be identical to the existing single-parameter `NavigateToStringAsync(string html)`.
The existing single-parameter overload SHALL delegate to the new overload with `baseUrl: null`.

#### Scenario: NavigateToStringAsync with baseUrl resolves relative resources
- **WHEN** `NavigateToStringAsync(html, baseUrl)` is called with a non-null `baseUrl`
- **THEN** relative resource references in the HTML resolve against the provided `baseUrl`

#### Scenario: NavigateToStringAsync without baseUrl preserves existing behavior
- **WHEN** `NavigateToStringAsync(html)` is called (single-parameter)
- **THEN** behavior is identical to calling `NavigateToStringAsync(html, null)`

### Requirement: Navigation error exception hierarchy
The Core assembly SHALL define the following exception subclasses under `WebViewNavigationException`:
- `WebViewNetworkException` — connectivity failures (DNS, unreachable host, connection lost, no internet)
- `WebViewSslException` — TLS/certificate-related failures (bad date, untrusted, unknown root, not yet valid)
- `WebViewTimeoutException` — request timeout failures

Each subclass SHALL inherit from `WebViewNavigationException` and SHALL preserve the `NavigationId` and `RequestUri` properties.
Existing `catch (WebViewNavigationException)` handlers SHALL continue to catch all navigation error subtypes.

#### Scenario: Network error produces WebViewNetworkException
- **WHEN** a navigation fails due to a network connectivity issue
- **THEN** the `NavigationCompleted.Error` is a `WebViewNetworkException` with `NavigationId` and `RequestUri` set

#### Scenario: SSL error produces WebViewSslException
- **WHEN** a navigation fails due to a TLS/certificate issue
- **THEN** the `NavigationCompleted.Error` is a `WebViewSslException` with `NavigationId` and `RequestUri` set

#### Scenario: Timeout error produces WebViewTimeoutException
- **WHEN** a navigation fails due to a request timeout
- **THEN** the `NavigationCompleted.Error` is a `WebViewTimeoutException` with `NavigationId` and `RequestUri` set

#### Scenario: Base exception catch still works
- **WHEN** a consumer catches `WebViewNavigationException`
- **THEN** it catches `WebViewNetworkException`, `WebViewSslException`, and `WebViewTimeoutException`
