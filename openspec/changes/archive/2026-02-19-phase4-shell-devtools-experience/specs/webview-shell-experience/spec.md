## ADDED Requirements

### Requirement: DevTools operations can be governed by shell policy
The shell experience component SHALL provide policy-governed DevTools operations (`open`, `close`, `query`) with deterministic allow/deny semantics.

#### Scenario: DevTools operation executes when policy allows
- **WHEN** a shell DevTools operation is invoked and policy allows the action
- **THEN** runtime executes the corresponding underlying `IWebView` DevTools operation

#### Scenario: DevTools operation is blocked when policy denies
- **WHEN** a shell DevTools operation is invoked and policy denies the action
- **THEN** runtime blocks the operation, reports explicit policy failure metadata, and returns deterministic blocked result

### Requirement: DevTools policy failures are isolated from other shell domains
DevTools policy denial or failure SHALL NOT break other shell policy domains.

#### Scenario: DevTools deny does not break permission governance
- **WHEN** a DevTools operation is denied by shell policy
- **THEN** download/permission/new-window/session domains continue to behave deterministically for subsequent events
