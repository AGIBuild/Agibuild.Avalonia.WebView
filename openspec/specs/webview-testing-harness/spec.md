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
- configuring a script result returned by `InvokeScriptAsync`
- raising adapter events deterministically for tests
- simulating navigation completion outcomes: `Success`, `Failure`, `Canceled`, `Superseded`
- simulating WebMessage inputs with controllable origin/protocol/channel metadata
- simulating native-initiated navigation starts by calling the adapter-host callback with a controllable correlation id

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
- script failure maps to `WebViewScriptException`
- WebMessage bridge is disabled by default
- WebMessage policy drops are observable with a drop reason
- native-initiated navigation can be denied via `NavigationStarted.Cancel`
- redirects reuse `CorrelationId` and are correlated to a single `NavigationId`
- command navigations (`GoBack/GoForward/Refresh`) are cancelable and correlated by `NavigationId`

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
