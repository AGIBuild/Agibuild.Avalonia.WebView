## Requirements

### Requirement: Core contracts assembly
The system SHALL provide a public contracts assembly named `Agibuild.Avalonia.WebView.Core` with root namespace `Agibuild.Avalonia.WebView`.
The assembly SHALL target `net10.0` and SHALL NOT reference any platform adapter projects.

#### Scenario: Core is platform-agnostic
- **WHEN** a project references `Agibuild.Avalonia.WebView.Core` only
- **THEN** it builds without any platform-specific adapter dependencies

### Requirement: IWebView contract surface
The `IWebView` interface SHALL define:
- properties: `Uri Source { get; set; }`, `bool CanGoBack { get; }`, `bool CanGoForward { get; }`, `Guid ChannelId { get; }`
- methods: `Task NavigateAsync(Uri uri)`, `Task NavigateToStringAsync(string html)`, `Task<string?> InvokeScriptAsync(string script)`
- commands: `bool GoBack()`, `bool GoForward()`, `bool Refresh()`, `bool Stop()`
- accessors: `ICookieManager? TryGetCookieManager()`, `ICommandManager? TryGetCommandManager()`
- events: `NavigationStarted`, `NavigationCompleted`, `NewWindowRequested`, `WebMessageReceived`, `WebResourceRequested`, `EnvironmentRequested`

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
- `IWebViewEnvironmentOptions` with `bool EnableDevTools { get; set; }`
- `INativeWebViewHandleProvider` with `IPlatformHandle? TryGetWebViewHandle()`

#### Scenario: Environment and handle types are resolvable
- **WHEN** a project references these types
- **THEN** it compiles without missing type errors

### Requirement: Event args types
The Core assembly SHALL define event args types:
- `NavigationStartingEventArgs` with `Guid NavigationId { get; }`, `Uri RequestUri { get; }` and `bool Cancel { get; set; }`
- `NavigationCompletedEventArgs` with `Guid NavigationId { get; }`, `Uri RequestUri { get; }`, `NavigationCompletedStatus Status { get; }`, and `Exception? Error { get; }`
- `NewWindowRequestedEventArgs`
- `WebMessageReceivedEventArgs` with `string Body { get; }`, `string Origin { get; }`, and `Guid ChannelId { get; }`
- `WebResourceRequestedEventArgs`
- `EnvironmentRequestedEventArgs`

#### Scenario: Event args are available
- **WHEN** a consumer references the event args types
- **THEN** all listed types are present in the Core assembly

### Requirement: Navigation and auth status enums
The Core assembly SHALL define the enums:
- `NavigationCompletedStatus` with members: `Success`, `Failure`, `Canceled`, `Superseded`
- `WebAuthStatus` with members: `Success`, `UserCancel`, `Timeout`, `Error`

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
The Core assembly SHALL define the types `ICookieManager`, `ICommandManager`, `AuthOptions`, and `WebAuthResult`.

#### Scenario: Auxiliary types are resolvable
- **WHEN** a project references these types
- **THEN** it compiles without missing type errors
