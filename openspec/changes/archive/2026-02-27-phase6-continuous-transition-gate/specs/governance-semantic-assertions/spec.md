## ADDED Requirements

### Requirement: Transition-gate semantic assertions SHALL evaluate lane-aware parity invariants
Governance semantic assertions MUST evaluate closeout transition gate parity as lane-aware invariants across `Ci` and `CiPublish`, including explicit mapping support for lane-context-specific target names.

#### Scenario: Lane-aware parity invariant passes
- **WHEN** both lanes satisfy all required transition-gate parity invariants
- **THEN** semantic assertions pass without relying on fragile textual matching

#### Scenario: Lane-aware parity invariant fails
- **WHEN** one or more transition-gate parity invariants are violated for a lane pair
- **THEN** semantic assertions fail deterministically with invariant-keyed diagnostics

### Requirement: Transition-gate diagnostics MUST include lane context and expected-vs-actual payload
Governance diagnostics for transition-gate semantic assertion failures SHALL include lane context, stable invariant identifier, affected artifact path, and expected-vs-actual payload values.

#### Scenario: Diagnostic payload is complete
- **WHEN** a transition-gate semantic assertion fails
- **THEN** failure output includes lane context, invariant id, artifact path, and expected-vs-actual values
