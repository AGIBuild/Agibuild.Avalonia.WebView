## ADDED Requirements

### Requirement: Adapter abstractions assembly
The system SHALL provide an assembly named `Agibuild.Avalonia.WebView.Adapters.Abstractions` targeting `net10.0`.
This assembly SHALL reference `Agibuild.Avalonia.WebView.Core` and SHALL NOT reference any platform-specific adapter projects.

#### Scenario: Abstractions are platform-free
- **WHEN** a project references `Agibuild.Avalonia.WebView.Adapters.Abstractions`
- **THEN** it builds without any platform-specific adapter dependencies

### Requirement: IWebViewAdapter contract surface
The `IWebViewAdapter` interface SHALL define:
- lifecycle: `void Initialize(IWebView host)`, `void Attach(IPlatformHandle parentHandle)`, `void Detach()`
- navigation: `Task NavigateAsync(Uri uri)`, `Task NavigateToStringAsync(string html)`
- scripting: `Task<string?> InvokeScriptAsync(string script)`
- commands: `bool GoBack()`, `bool GoForward()`, `bool Refresh()`, `bool Stop()`
- state: `bool CanGoBack { get; }`, `bool CanGoForward { get; }`
- events: `NavigationStarted`, `NavigationCompleted`, `NewWindowRequested`, `WebMessageReceived`, `WebResourceRequested`, `EnvironmentRequested`

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

### Requirement: Event parity between adapter and core
The adapter event args SHALL use the Core event args types (`NavigationStartingEventArgs`, `NavigationCompletedEventArgs`, `NewWindowRequestedEventArgs`, `WebMessageReceivedEventArgs`, `WebResourceRequestedEventArgs`, `EnvironmentRequestedEventArgs`).

#### Scenario: Adapter events use core event args
- **WHEN** a consumer inspects event signatures on `IWebViewAdapter`
- **THEN** each event uses the corresponding Core event args type
