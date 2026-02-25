## ADDED Requirements

### Requirement: DevTools open/close transitions SHALL be deterministic
Runtime DevTools control paths SHALL expose deterministic state transitions for open, close, and repeated calls.

#### Scenario: Repeated open call is idempotent
- **WHEN** `OpenDevTools` is invoked while DevTools is already open
- **THEN** call completes without throwing and state remains open

#### Scenario: Repeated close call is idempotent
- **WHEN** `CloseDevTools` is invoked while DevTools is already closed
- **THEN** call completes without throwing and state remains closed

### Requirement: macOS DevTools behavior SHALL be covered by deterministic governance tests
Governance/contract tests SHALL include deterministic assertions for macOS adapter DevTools behavior and unsupported-path semantics.

#### Scenario: macOS contract path remains stable
- **WHEN** DevTools governance tests run for shared runtime semantics
- **THEN** macOS path expectations are asserted for open/close/state transitions
