## MODIFIED Requirements

### Requirement: IWebView contract surface
The `IWebView` interface SHALL define:
- properties: `Uri Source { get; set; }`, `bool CanGoBack { get; }`, `bool CanGoForward { get; }`, `Guid ChannelId { get; }`
- methods: `Task NavigateAsync(Uri uri)`, `Task NavigateToStringAsync(string html)`, `Task NavigateToStringAsync(string html, Uri? baseUrl)`, `Task<string?> InvokeScriptAsync(string script)`
- commands: `bool GoBack()`, `bool GoForward()`, `bool Refresh()`, `bool Stop()`
- accessors: `ICookieManager? TryGetCookieManager()`, `ICommandManager? TryGetCommandManager()`
- events: `NavigationStarted`, `NavigationCompleted`, `NewWindowRequested`, `WebMessageReceived`, `WebResourceRequested`, `EnvironmentRequested`, `AdapterCreated`, `AdapterDestroyed`

#### Scenario: IWebView members are available
- **WHEN** a consumer reflects on `IWebView`
- **THEN** all listed members are present with the specified signatures

### Requirement: Event args types
The Core assembly SHALL define event args types:
- `NavigationStartingEventArgs` with `Guid NavigationId { get; }`, `Uri RequestUri { get; }` and `bool Cancel { get; set; }`
- `NavigationCompletedEventArgs` with `Guid NavigationId { get; }`, `Uri RequestUri { get; }`, `NavigationCompletedStatus Status { get; }`, and `Exception? Error { get; }`
- `NewWindowRequestedEventArgs`
- `WebMessageReceivedEventArgs` with `string Body { get; }`, `string Origin { get; }`, and `Guid ChannelId { get; }`
- `WebResourceRequestedEventArgs`
- `EnvironmentRequestedEventArgs`
- `AdapterCreatedEventArgs` with `IPlatformHandle? PlatformHandle { get; }`

#### Scenario: Event args are available
- **WHEN** a consumer references the event args types
- **THEN** all listed types are present in the Core assembly

### Requirement: Environment options and native handles
The Core assembly SHALL define:
- `IWebViewEnvironmentOptions` with `bool EnableDevTools { get; set; }`
- `INativeWebViewHandleProvider` with `IPlatformHandle? TryGetWebViewHandle()`
- `IWindowsWebView2PlatformHandle` extending `IPlatformHandle` with `nint CoreWebView2Handle` and `nint CoreWebView2ControllerHandle`
- `IAppleWKWebViewPlatformHandle` extending `IPlatformHandle` with `nint WKWebViewHandle`
- `IGtkWebViewPlatformHandle` extending `IPlatformHandle` with `nint WebKitWebViewHandle`
- `IAndroidWebViewPlatformHandle` extending `IPlatformHandle` with `nint AndroidWebViewHandle`

#### Scenario: Environment and handle types are resolvable
- **WHEN** a project references these types
- **THEN** it compiles without missing type errors
