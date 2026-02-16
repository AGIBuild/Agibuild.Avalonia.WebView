## Purpose
Define deterministic, platform-agnostic testing harness requirements for validating WebView contract behavior and threading semantics.

## Requirements

### Requirement: Test project scaffold
The solution SHALL include a test project named `Agibuild.Avalonia.WebView.UnitTests` targeting `net10.0`.
The test project SHALL reference `Agibuild.Avalonia.WebView.Core` and `Agibuild.Avalonia.WebView.Adapters.Abstractions`.

#### Scenario: Tests build without platform dependencies
- **WHEN** the test project is built
- **THEN** it compiles without any platform-specific adapter dependencies

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

### Requirement: Deterministic UI-thread dispatcher for contract tests
The test harness SHALL provide a deterministic dispatcher abstraction for tests that:
- can identify the current thread as the UI thread for assertions
- can marshal work to the UI thread deterministically without timing sleeps

#### Scenario: Off-thread calls are marshaled deterministically
- **WHEN** a contract test invokes an async WebView API from a non-UI thread
- **THEN** the harness can deterministically observe adapter invocations and public events on the UI thread

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

### Requirement: Contract test examples
The test project SHALL include contract test examples that exercise the mock adapter, including:
- a navigation-started event that can be cancelled
- script invocation returning a configured value

#### Scenario: Contract tests execute on mock adapter
- **WHEN** tests are executed against `MockWebViewAdapter`
- **THEN** the contract test examples pass deterministically

### Requirement: macOS WKWebView integration smoke tests exist (M0)
The system SHALL provide a macOS-only Integration Test (IT) smoke suite that exercises a real WKWebView-backed adapter end-to-end.

The smoke suite SHALL validate, at minimum:
- native-initiated navigation interception via `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
- redirect correlation behavior using a stable `CorrelationId` across a single redirect chain
- cancellation behavior where `Cancel=true` denies the native step and completes the correlated navigation as `Canceled`
- minimal script execution and WebMessage receive behavior (bridge enabled path)

#### Scenario: Smoke suite covers link click navigation
- **WHEN** a page in WKWebView triggers a main-frame navigation via a user link click
- **THEN** the IT suite observes `NavigationStarted` and `NavigationCompleted` for the same `NavigationId`

#### Scenario: Smoke suite covers 302 redirect correlation
- **WHEN** a main-frame navigation results in one or more HTTP 302 redirects within the same logical navigation chain
- **THEN** the adapter-host callback is invoked for each redirect step using the same `CorrelationId` and the final completion is reported exactly once for the host-issued `NavigationId`

#### Scenario: Smoke suite covers script-driven navigation
- **WHEN** a page triggers a main-frame navigation via `window.location`
- **THEN** the IT suite observes native-initiated navigation interception and a successful `NavigationCompleted`

#### Scenario: Smoke suite covers cancellation via NavigationStarted.Cancel
- **WHEN** an app handler sets `Cancel=true` for a native-initiated navigation step
- **THEN** the native step is denied and `NavigationCompleted` is raised with status `Canceled` for the corresponding `NavigationId`

#### Scenario: Smoke suite covers minimal script + WebMessage receive path
- **WHEN** the app invokes `InvokeScriptAsync` and the page posts a WebMessage on the configured channel
- **THEN** the IT suite observes a script result (when applicable) and a `WebMessageReceived` event for the instance channel

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

### Requirement: Windows WebView2 integration smoke tests exist (M0)
The system SHALL provide a Windows-only Integration Test (IT) smoke suite that exercises a real WebView2-backed adapter end-to-end.

The smoke suite SHALL validate, at minimum:
- native-initiated navigation interception via `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
- redirect correlation behavior using a stable `CorrelationId` across a single redirect chain
- cancellation behavior where `Cancel=true` denies the native step and completes the correlated navigation as `Canceled`
- minimal script execution and WebMessage receive behavior (bridge enabled path)
- cookie CRUD via `ICookieManager` backed by `CoreWebView2CookieManager`
- navigation error categorization mapping from `CoreWebView2WebErrorStatus`
- `TryGetWebViewHandle()` returning a valid handle with descriptor `"WebView2"`
- `NavigateToStringAsync(html, baseUrl)` with proper baseUrl resolution

#### Scenario: Smoke suite covers link click navigation
- **WHEN** a page in WebView2 triggers a main-frame navigation via a user link click
- **THEN** the IT suite observes `NavigationStarted` and `NavigationCompleted` for the same `NavigationId`

#### Scenario: Smoke suite covers redirect correlation
- **WHEN** a main-frame navigation results in one or more HTTP 302 redirects within the same logical navigation chain
- **THEN** the adapter-host callback is invoked for each redirect step using the same `CorrelationId` and the final completion is reported exactly once for the host-issued `NavigationId`

#### Scenario: Smoke suite covers script-driven navigation
- **WHEN** a page triggers a main-frame navigation via `window.location`
- **THEN** the IT suite observes native-initiated navigation interception and a successful `NavigationCompleted`

#### Scenario: Smoke suite covers cancellation via NavigationStarted.Cancel
- **WHEN** an app handler sets `Cancel=true` for a native-initiated navigation step
- **THEN** the native step is denied and `NavigationCompleted` is raised with status `Canceled` for the corresponding `NavigationId`

#### Scenario: Smoke suite covers script + WebMessage receive path
- **WHEN** the app invokes `InvokeScriptAsync` and the page posts a WebMessage on the configured channel
- **THEN** the IT suite observes a script result and a `WebMessageReceived` event for the instance channel

#### Scenario: Smoke suite covers cookie CRUD
- **WHEN** a cookie is set via `ICookieManager.SetCookieAsync`, navigated to a page, and then queried via `GetCookiesAsync`
- **THEN** the cookie is present in the result

#### Scenario: Smoke suite covers network error categorization
- **WHEN** a navigation targets an unreachable host on Windows
- **THEN** `NavigationCompleted.Error` is a `WebViewNetworkException`

#### Scenario: Smoke suite covers native handle
- **WHEN** the WebView2 adapter is attached
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "WebView2"`

#### Scenario: Smoke suite covers NavigateToStringAsync with baseUrl
- **WHEN** `NavigateToStringAsync(html, baseUrl)` is called with a non-null `baseUrl`
- **THEN** the loaded page resolves relative resources against `baseUrl` and the heading content is accessible via `InvokeScriptAsync`

### Requirement: Android WebView integration smoke tests exist (M0)
The system SHALL provide an Android-only Integration Test (IT) smoke suite that exercises a real Android WebView-backed adapter end-to-end.

The smoke suite SHALL validate, at minimum:
- native-initiated navigation interception via `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
- redirect correlation behavior using a stable `CorrelationId` across a single redirect chain
- cancellation behavior where `Cancel=true` denies the native step and completes the correlated navigation as `Canceled`
- minimal script execution and WebMessage receive behavior (bridge enabled path)
- cookie CRUD via `ICookieManager` backed by `android.webkit.CookieManager`
- navigation error categorization mapping from `WebViewClient.ERROR_*` codes
- `TryGetWebViewHandle()` returning a valid handle with descriptor `"AndroidWebView"`
- `NavigateToStringAsync(html, baseUrl)` with proper baseUrl resolution via `loadDataWithBaseURL`

#### Scenario: Smoke suite covers link click navigation
- **WHEN** a page in Android WebView triggers a main-frame navigation via a user link click
- **THEN** the IT suite observes `NavigationStarted` and `NavigationCompleted` for the same `NavigationId`

#### Scenario: Smoke suite covers redirect correlation
- **WHEN** a main-frame navigation results in one or more HTTP 302 redirects within the same logical navigation chain
- **THEN** the adapter-host callback is invoked with a stable `CorrelationId` and the final completion is reported exactly once for the host-issued `NavigationId`

#### Scenario: Smoke suite covers script-driven navigation
- **WHEN** a page triggers a main-frame navigation via `window.location`
- **THEN** the IT suite observes native-initiated navigation interception and a successful `NavigationCompleted`

#### Scenario: Smoke suite covers cancellation via Cancel
- **WHEN** an app handler sets `Cancel=true` for a native-initiated navigation step
- **THEN** the native step is denied and `NavigationCompleted` is raised with status `Canceled` for the corresponding `NavigationId`

#### Scenario: Smoke suite covers script + WebMessage receive path
- **WHEN** the app invokes `InvokeScriptAsync` and the page posts a WebMessage on the configured channel
- **THEN** the IT suite observes a script result and a `WebMessageReceived` event for the instance channel

#### Scenario: Smoke suite covers cookie CRUD
- **WHEN** a cookie is set via `ICookieManager.SetCookieAsync`, navigated to a page, and then queried via `GetCookiesAsync`
- **THEN** the cookie is present in the result

#### Scenario: Smoke suite covers network error categorization
- **WHEN** a navigation targets an unreachable host on Android
- **THEN** `NavigationCompleted.Error` is a `WebViewNetworkException`

#### Scenario: Smoke suite covers native handle
- **WHEN** the Android WebView adapter is attached
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "AndroidWebView"`

#### Scenario: Smoke suite covers NavigateToStringAsync with baseUrl
- **WHEN** `NavigateToStringAsync(html, baseUrl)` is called with a non-null `baseUrl`
- **THEN** the loaded page resolves relative resources against `baseUrl` and the heading content is accessible via `InvokeScriptAsync`

### Requirement: Repository test projects SHALL use xUnit v3 package baseline
All repository-owned test projects that currently depend on xUnit v2 packages SHALL migrate to `xunit.v3` while preserving compatibility with `dotnet test` execution in local and CI environments.

#### Scenario: Existing xUnit v2 references are replaced
- **WHEN** test project package references are reviewed after migration
- **THEN** scoped projects no longer reference `xunit` v2 and instead reference `xunit.v3`

#### Scenario: Deterministic test execution remains available
- **WHEN** `dotnet test` is executed for migrated test projects
- **THEN** test discovery and execution succeed without introducing runner-model changes outside the package migration scope
