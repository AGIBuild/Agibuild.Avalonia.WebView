## ADDED Requirements

### Requirement: Command shortcut execution can be governed by shell policy
The shell experience component SHALL provide policy-governed execution for standard editing commands (`Copy`, `Cut`, `Paste`, `SelectAll`, `Undo`, `Redo`) using a deterministic shell command entry point.

#### Scenario: Allowed command executes underlying command manager operation
- **WHEN** shell command policy allows a command action and command manager is available
- **THEN** runtime executes corresponding command manager operation and reports success

#### Scenario: Denied command does not execute underlying command manager operation
- **WHEN** shell command policy denies a command action
- **THEN** runtime does not execute command manager operation, returns deterministic failure, and emits policy failure metadata

### Requirement: Command policy failures are isolated from other shell domains
Command deny/failure SHALL NOT corrupt permission/download/new-window/session behavior.

#### Scenario: Command deny does not break permission governance
- **WHEN** command execution is denied by shell policy
- **THEN** subsequent permission requests continue to be processed deterministically according to configured permission policy
