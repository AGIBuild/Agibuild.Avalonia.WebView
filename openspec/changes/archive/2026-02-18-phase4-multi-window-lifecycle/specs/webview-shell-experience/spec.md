## MODIFIED Requirements

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

#### Scenario: External-browser strategy does not create managed window
- **WHEN** the policy is configured for external-browser and a new-window request occurs
- **THEN** shell runtime routes the target URI to external open execution and does not create a managed window

### Requirement: Shell policy execution order is deterministic
The shell experience foundation SHALL define deterministic execution order for policy domains and runtime fallback behavior.

#### Scenario: Policy decision is applied before fallback
- **WHEN** a shell policy handler is configured for an event domain
- **THEN** runtime applies the handler decision first and only uses fallback behavior when the handler leaves the event unhandled/default

#### Scenario: Runtime fallback remains deterministic
- **WHEN** handler output is absent or explicitly defers to baseline behavior
- **THEN** runtime uses the same fallback behavior for equivalent inputs

#### Scenario: New-window strategy resolution is evaluated before lifecycle execution
- **WHEN** a new-window policy is configured to use managed-window strategy
- **THEN** runtime finalizes strategy resolution before executing lifecycle state transitions for the target window
