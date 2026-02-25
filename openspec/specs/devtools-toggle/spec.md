## Purpose
Define devtools-toggle contracts for runtime API surface and adapter support behavior.

## Requirements

### Requirement: DevTools adapter facet contract is defined
Adapter abstractions SHALL define a devtools facet exposing open, close, and state-query operations.

#### Scenario: DevTools facet members are available
- **WHEN** consumers inspect adapter devtools contracts
- **THEN** open/close/state members are present with deterministic signatures

### Requirement: IWebView exposes devtools control surface
`IWebView` SHALL expose devtools open/close/state APIs and SHALL preserve deterministic no-op behavior when the active adapter does not provide devtools support.

#### Scenario: Unsupported adapter path is deterministic no-op
- **WHEN** devtools APIs are called on an adapter without devtools capability
- **THEN** calls complete without throwing and without side effects

### Requirement: Platform support matrix is explicit
Devtools behavior SHALL document platform-specific support, including supported Windows path and explicit no-op behavior on unsupported platforms.

#### Scenario: Platform behavior is predictable by contract
- **WHEN** platform-specific devtools behavior is reviewed
- **THEN** support and no-op expectations are explicitly documented

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
