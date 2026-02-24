## ADDED Requirements

### Requirement: Shell experience SHALL enforce v2 evaluation order for system actions
Shell experience SHALL evaluate system action requests in deterministic order: schema/whitelist validation first, then policy evaluation, then provider execution.

#### Scenario: Whitelist deny prevents policy/provider execution
- **WHEN** a system action request uses action not allowed by whitelist v2
- **THEN** shell returns deterministic deny and does not invoke policy/provider execution path

#### Scenario: Policy deny prevents provider execution after whitelist pass
- **WHEN** a system action request passes whitelist v2 and policy denies
- **THEN** shell returns deterministic deny and provider execution count remains zero

### Requirement: Shell experience SHALL isolate tray payload v2 validation failures
Tray payload v2 validation failures SHALL NOT break other shell policy domains.

#### Scenario: Tray payload validation failure does not break permission governance
- **WHEN** tray payload v2 validation fails in system integration inbound path
- **THEN** subsequent permission/download/new-window flows continue deterministic policy behavior
