## Purpose
Define shell session scope/isolation contracts that hosts can opt into as part of application shell governance, while remaining platform-agnostic and testable.
## Requirements
### Requirement: Session policy is explicit and opt-in
The system SHALL define a shell session policy contract that allows hosts to explicitly select session scope behavior for shell-governed WebView usage.

#### Scenario: Shell session policy is not applied by default
- **WHEN** a host does not configure shell session policy
- **THEN** runtime behavior remains consistent with existing default WebView environment/session semantics

#### Scenario: Host selects shared session scope
- **WHEN** a host configures shell session policy to shared scope
- **THEN** shell-governed views use the same session context within that host-defined scope

#### Scenario: Host selects isolated session scope
- **WHEN** a host configures shell session policy to isolated scope
- **THEN** shell-governed views use an isolated session context as defined by the configured policy

### Requirement: Session policy is deterministic and testable
Session policy evaluation SHALL be deterministic and testable via contract tests without requiring a real browser process.

#### Scenario: Session policy evaluation is stable for the same inputs
- **WHEN** the same session policy and the same shell context inputs are evaluated multiple times
- **THEN** the resulting session scope decision is identical each time

#### Scenario: Session policy behavior is contract-testable
- **WHEN** contract tests execute against MockAdapter/TestDispatcher
- **THEN** session policy decisions can be validated without platform-specific WebView dependencies

### Requirement: Session policy supports future multi-window composition
The session policy contract SHALL carry enough identity/context to compose with multi-window orchestration and session-permission profile governance, including window and parent-window relationship context.

#### Scenario: Session policy includes host-defined scope identity
- **WHEN** a host uses shell session policy in a multi-view workflow
- **THEN** the policy contract provides a host-defined scope identity that can be reused by window orchestration and profile resolution logic

#### Scenario: Session policy receives parent-child window context
- **WHEN** runtime evaluates session policy for a child window request
- **THEN** the policy context includes parent window identity so the policy can decide inheritance or isolation deterministically

### Requirement: Session inheritance across managed windows is explicit
The runtime SHALL use explicit session inheritance semantics across parent-child managed windows; inheritance MUST be policy/profile-driven rather than implicit platform behavior.

#### Scenario: Policy chooses inheritance to parent session scope
- **WHEN** a child window request is evaluated and policy resolves inheritance
- **THEN** child window uses the same session scope identity as the parent context

#### Scenario: Policy chooses isolated child session scope
- **WHEN** a child window request is evaluated and policy resolves isolation
- **THEN** child window uses a distinct session scope identity from the parent context

### Requirement: Session decision correlation is stable per window identity
Session decisions SHALL be correlated to stable window identities for diagnostics and lifecycle assertions.

#### Scenario: Session decision can be traced for each managed window
- **WHEN** runtime emits lifecycle and session diagnostics for managed windows
- **THEN** each session decision can be correlated by window id and scope identity deterministically

### Requirement: Session decisions correlate to resolved profile identity
Session decisions SHALL correlate with resolved session-permission profile identity for auditable governance behavior.

#### Scenario: Session decision audit includes profile identity
- **WHEN** runtime emits diagnostics for a resolved session decision
- **THEN** diagnostics include both session scope identity and profile identity for the window context

