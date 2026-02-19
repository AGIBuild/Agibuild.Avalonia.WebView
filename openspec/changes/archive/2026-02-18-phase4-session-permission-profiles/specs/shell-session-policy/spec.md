## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Session decisions correlate to resolved profile identity
Session decisions SHALL correlate with resolved session-permission profile identity for auditable governance behavior.

#### Scenario: Session decision audit includes profile identity
- **WHEN** runtime emits diagnostics for a resolved session decision
- **THEN** diagnostics include both session scope identity and profile identity for the window context
