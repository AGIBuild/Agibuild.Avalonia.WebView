## MODIFIED Requirements

### Requirement: Host capability bridge is typed and opt-in
The system SHALL provide an opt-in typed host capability bridge with explicit operations for clipboard, file dialog, external open, notification, application menu, system tray, and supported system actions.

#### Scenario: Host capability bridge is disabled by default
- **WHEN** an app does not configure a host capability bridge
- **THEN** existing shell/runtime behavior remains unchanged and no host capability calls are executed

#### Scenario: Typed clipboard operation returns typed result
- **WHEN** host code invokes clipboard read/write through the capability bridge
- **THEN** the operation returns typed success/failure semantics without stringly-typed command routing

#### Scenario: Typed menu operation returns deterministic outcome
- **WHEN** host code invokes menu update operation through the capability bridge
- **THEN** runtime returns deterministic allow/deny/failure outcome with stable metadata

#### Scenario: Typed tray operation returns deterministic outcome
- **WHEN** host code invokes tray state operation through the capability bridge
- **THEN** runtime returns deterministic allow/deny/failure outcome with stable metadata

### Requirement: Capability bridge behavior is contract-testable and integration-testable
The capability bridge SHALL be fully testable in contract tests with MockAdapter and validated by focused integration tests, including system integration capability paths.

#### Scenario: Contract tests validate allow/deny policy matrix
- **WHEN** contract tests execute capability bridge calls with deterministic policy stubs
- **THEN** allow and deny branches for each capability type are validated without platform dependencies

#### Scenario: Integration tests validate representative desktop capability flow
- **WHEN** integration automation runs representative capability operations
- **THEN** typed results and policy enforcement behavior pass deterministically

#### Scenario: Contract tests validate menu/tray capability matrix
- **WHEN** contract tests execute typed menu/tray operations with policy allow and deny branches
- **THEN** provider execution count and diagnostic outcomes are deterministic and machine-checkable
