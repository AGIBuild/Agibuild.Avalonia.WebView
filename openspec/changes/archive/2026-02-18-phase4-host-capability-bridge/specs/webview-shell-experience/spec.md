## MODIFIED Requirements

### Requirement: Shell experience is opt-in and non-breaking
The system SHALL provide an opt-in shell policy foundation that improves common host behaviors (new-window, downloads, permissions) and optional host capability bridge integration without changing baseline WebView contract semantics when not enabled.

#### Scenario: Default runtime behavior is unchanged when shell experience is not enabled
- **WHEN** an app uses `Agibuild.Fulora` without enabling shell experience
- **THEN** the baseline behaviors defined by existing specs remain unchanged

#### Scenario: Host capability bridge is optional in shell experience
- **WHEN** shell experience is enabled without host capability bridge configuration
- **THEN** shell policy domains continue to work without host capability execution

### Requirement: New window policy strategies are configurable
The shell experience component SHALL provide a configurable policy for `NewWindowRequested` with at least the following strategies:
- navigate in the current view
- delegate to host-provided callback
- open a runtime-managed secondary window
- open in an external browser

#### Scenario: Navigate-in-place strategy handles NewWindowRequested
- **WHEN** the policy is configured to navigate in the current view and a new-window request occurs with a non-null target URI
- **THEN** the current view navigates to that URI in-place (via existing v1 fallback semantics) and no new window is opened

#### Scenario: Delegate strategy routes the decision to host code
- **WHEN** the policy is configured to delegate and a new-window request occurs
- **THEN** the host callback is invoked with the target URI and can mark the request handled

#### Scenario: Managed-window strategy routes request into lifecycle orchestrator
- **WHEN** the policy is configured for managed-window and a new-window request occurs
- **THEN** shell runtime routes the request to the managed-window lifecycle orchestrator with deterministic window identity assignment

#### Scenario: External-browser strategy routes through host capability bridge when configured
- **WHEN** the policy is configured for external-browser and host capability bridge is enabled
- **THEN** shell runtime routes the target URI to typed external-open capability execution with authorization policy enforcement
