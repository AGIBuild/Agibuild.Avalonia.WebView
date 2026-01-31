## ADDED Requirements

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

#### Scenario: Mock adapter supports deterministic behavior
- **WHEN** a test sets a script result and triggers a navigation event on the mock
- **THEN** the mock returns the configured script result and emits the expected event

### Requirement: Contract test examples
The test project SHALL include contract test examples that exercise the mock adapter, including:
- a navigation-started event that can be cancelled
- script invocation returning a configured value

#### Scenario: Contract tests execute on mock adapter
- **WHEN** tests are executed against `MockWebViewAdapter`
- **THEN** the contract test examples pass deterministically
