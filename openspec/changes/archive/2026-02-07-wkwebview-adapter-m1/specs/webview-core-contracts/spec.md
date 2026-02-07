## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Auxiliary core types
The Core assembly SHALL define the types `ICookieManager`, `ICommandManager`, `AuthOptions`, `WebAuthResult`, and `WebViewCookie`.
`ICookieManager` SHALL define async CRUD operations: `GetCookiesAsync`, `SetCookieAsync`, `DeleteCookieAsync`, `ClearAllCookiesAsync`.

#### Scenario: Auxiliary types are resolvable
- **WHEN** a project references these types
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

### Requirement: IWebView contract surface
The `IWebView` interface SHALL define:
- properties: `Uri Source { get; set; }`, `bool CanGoBack { get; }`, `bool CanGoForward { get; }`, `Guid ChannelId { get; }`
- methods: `Task NavigateAsync(Uri uri)`, `Task NavigateToStringAsync(string html)`, `Task NavigateToStringAsync(string html, Uri? baseUrl)`, `Task<string?> InvokeScriptAsync(string script)`
- commands: `bool GoBack()`, `bool GoForward()`, `bool Refresh()`, `bool Stop()`
- accessors: `ICookieManager? TryGetCookieManager()`, `ICommandManager? TryGetCommandManager()`
- events: `NavigationStarted`, `NavigationCompleted`, `NewWindowRequested`, `WebMessageReceived`, `WebResourceRequested`, `EnvironmentRequested`

#### Scenario: IWebView members are available
- **WHEN** a consumer reflects on `IWebView`
- **THEN** all listed members are present with the specified signatures
