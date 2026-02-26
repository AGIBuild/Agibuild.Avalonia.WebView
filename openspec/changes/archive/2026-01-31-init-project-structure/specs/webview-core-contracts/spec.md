## ADDED Requirements

### Requirement: Core contracts assembly
The system SHALL provide a public contracts assembly named `Agibuild.Fulora.Core` with root namespace `Agibuild.Fulora`.
The assembly SHALL target `net10.0` and SHALL NOT reference any platform adapter projects.

#### Scenario: Core is platform-agnostic
- **WHEN** a project references `Agibuild.Fulora.Core` only
- **THEN** it builds without any platform-specific adapter dependencies

### Requirement: IWebView contract surface
The `IWebView` interface SHALL define:
- properties: `Uri Source { get; set; }`, `bool CanGoBack { get; }`, `bool CanGoForward { get; }`
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
- `NavigationStartingEventArgs` with `Uri RequestUri { get; }` and `bool Cancel { get; set; }`
- `NavigationCompletedEventArgs`
- `NewWindowRequestedEventArgs`
- `WebMessageReceivedEventArgs`
- `WebResourceRequestedEventArgs`
- `EnvironmentRequestedEventArgs`

#### Scenario: Event args are available
- **WHEN** a consumer references the event args types
- **THEN** all listed types are present in the Core assembly

### Requirement: Auxiliary core types
The Core assembly SHALL define the types `ICookieManager`, `ICommandManager`, `AuthOptions`, and `WebAuthResult`.

#### Scenario: Auxiliary types are resolvable
- **WHEN** a project references these types
- **THEN** it compiles without missing type errors
