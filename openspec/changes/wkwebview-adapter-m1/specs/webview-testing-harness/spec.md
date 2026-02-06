## ADDED Requirements

### Requirement: MockWebViewAdapter supports ICookieAdapter
The `MockWebViewAdapter` SHALL optionally implement `ICookieAdapter` with:
- an in-memory cookie store for deterministic testing
- `GetCookiesAsync` returning cookies matching the URI's domain and path
- `SetCookieAsync` adding or replacing cookies by name+domain+path key
- `DeleteCookieAsync` removing cookies by name+domain+path key
- `ClearAllCookiesAsync` clearing all stored cookies

The mock cookie support SHALL be toggleable (enabled/disabled) to test both code paths of `TryGetCookieManager()`.

#### Scenario: Mock cookie store supports CRUD
- **WHEN** a test sets a cookie via the mock and then queries it
- **THEN** the mock returns the expected cookie

#### Scenario: Mock cookie support can be disabled
- **WHEN** the mock is configured without cookie support
- **THEN** the runtime's `TryGetCookieManager()` returns `null`

### Requirement: MockWebViewAdapter supports NewWindowRequested simulation
The `MockWebViewAdapter` SHALL provide a method to simulate a new-window request:
- `RaiseNewWindowRequested(Uri? uri)` — raises `NewWindowRequested` with the given URI

This enables CT for the unhandled-fallback navigation behavior.

#### Scenario: New window simulation triggers event
- **WHEN** a test calls `RaiseNewWindowRequested(uri)`
- **THEN** the adapter raises `NewWindowRequested` with the specified URI

### Requirement: MockWebViewAdapter supports categorized error simulation
The `MockWebViewAdapter` SHALL support raising `NavigationCompleted` with categorized exceptions:
- `RaiseNavigationCompleted(Guid navigationId, WebViewNetworkException error)`
- `RaiseNavigationCompleted(Guid navigationId, WebViewSslException error)`
- `RaiseNavigationCompleted(Guid navigationId, WebViewTimeoutException error)`

This enables CT for error categorization pass-through in the runtime.

#### Scenario: Mock raises categorized navigation error
- **WHEN** a test calls `RaiseNavigationCompleted` with a `WebViewNetworkException`
- **THEN** the adapter raises `NavigationCompleted` with `Status == Failure` and the specific exception type

### Requirement: MockWebViewAdapter supports NavigateToStringAsync with baseUrl
The `MockWebViewAdapter` SHALL record the `baseUrl` parameter when `NavigateToStringAsync(navigationId, html, baseUrl)` is called, enabling CT assertions on baseUrl pass-through.

#### Scenario: Mock records baseUrl
- **WHEN** a test invokes `NavigateToStringAsync` with a non-null `baseUrl` through the runtime
- **THEN** the mock adapter's recorded `LastBaseUrl` equals the provided `baseUrl`

## MODIFIED Requirements

### Requirement: Mock adapter for TDD
The test project SHALL include a `MockWebViewAdapter` that implements `IWebViewAdapter` and supports:
- storing the last navigation `Uri`
- storing the last `baseUrl` for `NavigateToStringAsync` overloads
- configuring a script result returned by `InvokeScriptAsync`
- raising adapter events deterministically for tests
- simulating navigation completion outcomes: `Success`, `Failure`, `Canceled`, `Superseded`
- simulating navigation completion with categorized exceptions: `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`
- simulating WebMessage inputs with controllable origin/protocol/channel metadata
- simulating native-initiated navigation starts by calling the adapter-host callback with a controllable correlation id
- simulating new-window requests with controllable URI
- optionally implementing `ICookieAdapter` with an in-memory cookie store

#### Scenario: Mock adapter supports deterministic behavior
- **WHEN** a test sets a script result and triggers a navigation event on the mock
- **THEN** the mock returns the configured script result and emits the expected event deterministically

### Requirement: Baseline contract test suite coverage
The unit test project SHALL include a deterministic Contract Test (CT) suite that covers, at minimum:
- all public events are raised on the UI thread
- `NavigationCompleted` is exactly-once per `NavigationId`
- canceling in `NavigationStarted` prevents adapter navigation and completes with `Canceled`
- Latest-wins: a second navigation causes the first to complete with `Superseded`
- navigation failure maps to `WebViewNavigationException`
- categorized navigation failures map to `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`
- script failure maps to `WebViewScriptException`
- WebMessage bridge is disabled by default
- WebMessage policy drops are observable with a drop reason
- native-initiated navigation can be denied via `NavigationStarted.Cancel`
- redirects reuse `CorrelationId` and are correlated to a single `NavigationId`
- command navigations (`GoBack/GoForward/Refresh`) are cancelable and correlated by `NavigationId`
- `NewWindowRequested` unhandled fallback navigates in current view
- `NewWindowRequested` handled does not trigger fallback navigation
- `TryGetCookieManager()` returns non-null when adapter supports `ICookieAdapter`
- `TryGetCookieManager()` returns null when adapter does not support `ICookieAdapter`
- cookie CRUD operations work via mock in-memory store
- `NavigateToStringAsync(html, baseUrl)` sets `Source` to `baseUrl` when non-null

#### Scenario: WebMessage policy denial is testable
- **WHEN** a contract test configures a mock WebMessage policy to deny a message
- **THEN** the message is dropped and `WebMessageReceived` is not raised

#### Scenario: Drop diagnostics are assertable in CT
- **WHEN** a message is dropped due to a policy denial
- **THEN** the test can assert the emitted `WebMessageDropReason` and associated metadata

#### Scenario: Baseline CT suite passes deterministically
- **WHEN** the CT suite is executed
- **THEN** it passes deterministically without platform dependencies
