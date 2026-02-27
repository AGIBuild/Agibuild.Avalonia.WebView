## MODIFIED Requirements

### Requirement: IWebDialog contract surface
The `IWebDialog` interface SHALL inherit from `IWebView` and define:
- properties: `string? Title { get; set; }`, `bool CanUserResize { get; set; }`
- methods: `void Show()`, `bool Show(INativeHandle owner)`, `void Close()`, `bool Resize(int width, int height)`, `bool Move(int x, int y)`
- events: `Closing`

#### Scenario: IWebDialog members are available
- **WHEN** a consumer reflects on `IWebDialog`
- **THEN** all listed members are present with the specified signatures

### Requirement: Environment options and native handles
The Core assembly SHALL define:
- `IWebViewEnvironmentOptions` with `bool EnableDevTools { get; set; }`, `IReadOnlyList<CustomSchemeRegistration>? CustomSchemes { get; set; }`
- `INativeHandle` with `nint Handle` and `string HandleDescriptor`
- `INativeWebViewHandleProvider` with `INativeHandle? TryGetWebViewHandle()`
- `IWindowsWebView2PlatformHandle` extending `INativeHandle` with `nint CoreWebView2Handle` and `nint CoreWebView2ControllerHandle`
- `IAppleWKWebViewPlatformHandle` extending `INativeHandle` with `nint WKWebViewHandle`
- `IGtkWebViewPlatformHandle` extending `INativeHandle` with `nint WebKitWebViewHandle`
- `IAndroidWebViewPlatformHandle` extending `INativeHandle` with `nint AndroidWebViewHandle`

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
- `AdapterCreatedEventArgs` with `INativeHandle? PlatformHandle { get; }`

#### Scenario: Event args are available
- **WHEN** a consumer references the event args types
- **THEN** all listed types are present in the Core assembly
