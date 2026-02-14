## ADDED Requirements

### Requirement: Async-boundary API audit is explicit and actionable
API surface review SHALL include explicit async-boundary audit items for runtime and control APIs where sync/async ambiguity exists.

#### Scenario: Audit captures native-handle boundary
- **WHEN** API surface review runs for the current release train
- **THEN** it records migration status for native handle access toward async-first contracts

### Requirement: Public event subscription lifecycle is audited
API surface review SHALL verify that control-level event subscriptions behave deterministically before and after core attach.

#### Scenario: Review captures pre-attach subscription semantics
- **WHEN** event forwarding APIs are audited
- **THEN** the report includes pass/fail outcomes for pre-attach subscribe/unsubscribe behavior

### Requirement: Blocking-wait exceptions are documented with owner and rationale
API surface review SHALL maintain an allowlist record for production blocking waits, including owning component and audited justification.

#### Scenario: New blocking wait is proposed
- **WHEN** a new `GetAwaiter().GetResult()` call is introduced in production source
- **THEN** review fails unless allowlist entry and rationale are added in the same change
