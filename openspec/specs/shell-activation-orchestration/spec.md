## ADDED Requirements

### Requirement: Shell activation orchestration SHALL provide deterministic primary-instance ownership
Shell runtime MUST provide a deterministic in-process coordinator that allows exactly one primary activation owner per app identity at a time.

#### Scenario: First registration becomes primary
- **WHEN** the first coordinator registers for an app identity
- **THEN** registration is accepted as primary owner

#### Scenario: Secondary registration is non-primary
- **WHEN** another coordinator registers while a primary owner is active
- **THEN** registration is marked as secondary and cannot claim ownership until primary is released

### Requirement: Secondary activation SHALL be forwarded to active primary handler
Secondary instances MUST be able to forward activation payload (including deep-link URI) to active primary handler with deterministic success/failure semantics.

#### Scenario: Forward activation to active primary succeeds
- **WHEN** a secondary registration forwards a deep-link activation while primary handler is active
- **THEN** primary handler receives the payload exactly once
- **AND** forward operation reports success

#### Scenario: Forward activation fails when no active primary exists
- **WHEN** no active primary owner exists for app identity
- **THEN** forward operation reports deterministic failure without side effects

### Requirement: Ownership lifecycle release SHALL be explicit and recoverable
Primary ownership release MUST allow next registration to become primary deterministically.

#### Scenario: Released primary allows takeover
- **WHEN** current primary registration is disposed/released
- **THEN** a subsequent registration can acquire primary ownership
