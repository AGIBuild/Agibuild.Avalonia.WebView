## ADDED Requirements

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
