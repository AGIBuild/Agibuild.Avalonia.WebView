## Requirements

### Requirement: Adapter abstractions assembly
The system SHALL provide an assembly named `Agibuild.Avalonia.WebView.Adapters.Abstractions` targeting `net10.0`.
This assembly SHALL reference `Agibuild.Avalonia.WebView.Core` and SHALL NOT reference any platform-specific adapter projects.

#### Scenario: Abstractions are platform-free
- **WHEN** a project references `Agibuild.Avalonia.WebView.Adapters.Abstractions`
- **THEN** it builds without any platform-specific adapter dependencies

### Requirement: IWebViewAdapter contract surface
The `IWebViewAdapter` interface SHALL define:
- lifecycle: `void Initialize(IWebViewAdapterHost host)`, `void Attach(IPlatformHandle parentHandle)`, `void Detach()`
- navigation: `Task NavigateAsync(Guid navigationId, Uri uri)`, `Task NavigateToStringAsync(Guid navigationId, string html)`
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
