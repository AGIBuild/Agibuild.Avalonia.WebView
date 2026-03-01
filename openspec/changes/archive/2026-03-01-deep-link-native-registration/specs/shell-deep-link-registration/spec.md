## ADDED Requirements

### Requirement: Deep-link registration SHALL expose typed app-scoped declarations
The runtime MUST provide a typed registration contract for host-declared deep-link scheme and route declarations bound to an app identity.

#### Scenario: Valid registration declaration is accepted
- **WHEN** host submits a declaration with valid app identity, scheme, and route constraints
- **THEN** runtime stores the declaration deterministically for activation admission

#### Scenario: Invalid declaration is rejected deterministically
- **WHEN** host submits a declaration with missing app identity or unsupported scheme format
- **THEN** runtime rejects the declaration with deterministic validation metadata

### Requirement: Native activation payload SHALL be canonicalized before orchestration dispatch
Native deep-link entry payloads MUST be normalized into a canonical activation envelope before dispatch to orchestration.

#### Scenario: Equivalent native payloads produce equivalent canonical envelope
- **WHEN** runtime receives semantically equivalent native activation payloads with different URI formatting
- **THEN** runtime produces the same canonical route and normalized URI fields

### Requirement: Native activation admission SHALL be policy-governed
The runtime MUST evaluate deep-link activation envelopes against policy constraints before orchestration dispatch.

#### Scenario: Allowed activation is dispatched
- **WHEN** canonical activation envelope satisfies configured scheme and route policy
- **THEN** runtime forwards activation to orchestration dispatch path

#### Scenario: Denied activation is blocked before dispatch
- **WHEN** canonical activation envelope violates configured scheme or route policy
- **THEN** runtime rejects activation deterministically and does not dispatch to handlers

### Requirement: Native activation ingress SHALL provide idempotent delivery semantics
The runtime MUST enforce deterministic idempotency for duplicate activation envelopes within the declared replay window.

#### Scenario: Duplicate activation within replay window is not redispatched
- **WHEN** runtime receives a duplicate canonical envelope with the same idempotency identity within replay window
- **THEN** runtime returns deterministic duplicate outcome and dispatch count remains unchanged
