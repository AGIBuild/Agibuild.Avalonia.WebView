## ADDED Requirements

### Requirement: Governance assertions MUST evaluate structured invariants
Governance tests SHALL validate structured invariants from machine-readable artifacts (for example JSON documents, capability mappings, and target dependency relations) instead of relying on fragile textual snippet matching.

#### Scenario: Structured invariant passes
- **WHEN** governed artifacts satisfy required schema fields and cross-artifact mapping rules
- **THEN** governance assertions pass without depending on source-code string literals

#### Scenario: Structured invariant violation fails deterministically
- **WHEN** a required invariant (such as missing mapping, invalid token, or schema mismatch) is detected
- **THEN** governance fails with deterministic machine-readable diagnostics identifying the violated invariant key

### Requirement: Governance diagnostics MUST be actionable
Governance failure output SHALL include a stable invariant identifier, affected artifact path, and expected-vs-actual summary so CI agents and maintainers can resolve failures without manual trace reconstruction.

#### Scenario: Failure output includes invariant metadata
- **WHEN** a semantic governance assertion fails
- **THEN** the emitted diagnostic contains invariant id, artifact location, and expected-vs-actual details
