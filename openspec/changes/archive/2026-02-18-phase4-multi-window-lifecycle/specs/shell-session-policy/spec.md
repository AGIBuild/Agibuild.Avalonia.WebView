## MODIFIED Requirements

### Requirement: Session policy supports future multi-window composition
The session policy contract SHALL carry enough identity/context to compose with future multi-window orchestration milestones, including window and parent-window relationship context.

#### Scenario: Session policy includes host-defined scope identity
- **WHEN** a host uses shell session policy in a multi-view workflow
- **THEN** the policy contract provides a host-defined scope identity that can be reused by future window orchestration logic

#### Scenario: Session policy receives parent-child window context
- **WHEN** runtime evaluates session policy for a child window request
- **THEN** the policy context includes parent window identity so the policy can decide inheritance or isolation deterministically

## ADDED Requirements

### Requirement: Session inheritance across managed windows is explicit
The runtime SHALL use explicit session inheritance semantics across parent-child managed windows; inheritance MUST be policy-driven rather than implicit platform behavior.

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
