## Purpose
Define deterministic in-process shell activation orchestration for deep-link routing and single-instance ownership handoff.

## Requirements

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

### Requirement: Orchestration SHALL dispatch canonical native activation while preserving ownership semantics
Shell activation orchestration MUST accept canonical native activation envelopes and dispatch them through the active primary-owner path without violating primary/secondary ownership guarantees.

#### Scenario: Native activation is delivered to active primary owner
- **WHEN** a canonical native activation envelope is admitted and a primary owner is active
- **THEN** primary activation handler receives the payload exactly once
- **AND** orchestration reports deterministic success

#### Scenario: Native activation fails deterministically without active primary owner
- **WHEN** a canonical native activation envelope is admitted and no active primary owner exists
- **THEN** orchestration reports deterministic failure without side effects

### Requirement: Orchestration SHALL remain deterministic under duplicate ingress and secondary forwarding overlap
Orchestration MUST preserve exactly-once dispatch semantics when equivalent activation arrives through both native ingress and secondary-instance forwarding paths.

#### Scenario: Equivalent ingress and forward payloads do not cause duplicate dispatch
- **WHEN** equivalent canonical activation payload is observed from native ingress and secondary forwarding within replay window
- **THEN** orchestration dispatches at most once to primary handler
- **AND** duplicate path returns deterministic duplicate outcome metadata
