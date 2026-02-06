## MODIFIED Requirements

### Requirement: Mock adapter for TDD
The test project SHALL include a `MockWebViewAdapter` that implements `IWebViewAdapter` and supports:
- storing the last navigation `Uri`
- configuring a script result returned by `InvokeScriptAsync`
- raising adapter events deterministically for tests
- simulating navigation completion outcomes: `Success`, `Failure`, `Canceled`, `Superseded`
- simulating WebMessage inputs with controllable origin/protocol/channel metadata

#### Scenario: Mock adapter supports deterministic behavior
- **WHEN** a test sets a script result and triggers a navigation event on the mock
- **THEN** the mock returns the configured script result and emits the expected event deterministically

## ADDED Requirements

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

#### Scenario: WebMessage policy denial is testable
- **WHEN** a contract test configures a mock WebMessage policy to deny a message
- **THEN** the message is dropped and `WebMessageReceived` is not raised

#### Scenario: Drop diagnostics are assertable in CT
- **WHEN** a message is dropped due to a policy denial
- **THEN** the test can assert the emitted `WebMessageDropReason` and associated metadata

#### Scenario: Baseline CT suite passes deterministically
- **WHEN** the CT suite is executed
- **THEN** it passes deterministically without platform dependencies

