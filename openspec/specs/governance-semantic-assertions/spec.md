# governance-semantic-assertions Specification

## Purpose
Define governance assertion requirements for structured invariant validation and actionable failure diagnostics, enabling machine-readable governance tests that avoid fragile textual matching and support CI agent resolution.

## Requirements
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

### Requirement: Phase transition governance SHALL be asserted by invariant IDs
Governance checks for roadmap/release closeout transitions MUST use stable invariant IDs and structured artifact fields instead of direct phase-title string coupling.

#### Scenario: Phase transition invariants pass
- **WHEN** roadmap status, closeout snapshot metadata, and governance mapping satisfy transition rules
- **THEN** governance assertions pass using stable invariant IDs without relying on phase-title literals

#### Scenario: Missing transition invariant metadata fails deterministically
- **WHEN** any required transition invariant field or mapping is absent
- **THEN** governance fails with deterministic diagnostics including invariant id, artifact path, and expected-vs-actual summary
