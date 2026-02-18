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
The session policy contract SHALL carry enough identity/context to compose with future multi-window orchestration milestones.

#### Scenario: Session policy includes host-defined scope identity
- **WHEN** a host uses shell session policy in a multi-view workflow
- **THEN** the policy contract provides a host-defined scope identity that can be reused by future window orchestration logic
