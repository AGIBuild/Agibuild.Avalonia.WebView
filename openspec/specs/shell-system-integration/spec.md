# shell-system-integration Specification

## Purpose
TBD - created by archiving change shell-system-integration-extension. Update Purpose after archive.
## Requirements
### Requirement: System integration capabilities SHALL be typed and policy-governed
The system SHALL expose menu, tray, and system-action operations through typed capability contracts evaluated by policy before provider execution.

#### Scenario: Allowed system integration request executes provider
- **WHEN** a typed system integration request is submitted and policy returns allow
- **THEN** runtime executes the mapped provider operation and returns deterministic `allow` outcome

#### Scenario: Denied system integration request skips provider
- **WHEN** a typed system integration request is submitted and policy returns deny
- **THEN** runtime does not execute provider logic and returns deterministic `deny` outcome with reason metadata

### Requirement: Menu model updates SHALL be deterministic
The system SHALL support typed menu model updates with deterministic replacement semantics for equivalent inputs.

#### Scenario: Equivalent menu models produce stable runtime state
- **WHEN** host applies the same typed menu model payload repeatedly
- **THEN** runtime state remains stable and does not create duplicate menu entries

### Requirement: Tray state operations SHALL be explicit and auditable
The system SHALL provide typed tray create/update/visibility operations with structured diagnostic events.

#### Scenario: Tray visibility change emits diagnostics
- **WHEN** tray visibility is updated through typed capability call
- **THEN** runtime emits machine-checkable diagnostics containing operation type, outcome, and correlation metadata

### Requirement: System actions SHALL use explicit allowlists
The system SHALL execute system actions only from an explicit typed action allowlist.

#### Scenario: Unknown system action is rejected deterministically
- **WHEN** a system action request references an unsupported action identifier
- **THEN** runtime returns deterministic `failure` or `deny` according to policy contract and does not execute host action

