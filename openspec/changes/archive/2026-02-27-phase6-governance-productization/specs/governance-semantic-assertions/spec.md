## ADDED Requirements

### Requirement: Phase transition governance SHALL be asserted by invariant IDs
Governance checks for roadmap/release closeout transitions MUST use stable invariant IDs and structured artifact fields instead of direct phase-title string coupling.

#### Scenario: Phase transition invariants pass
- **WHEN** roadmap status, closeout snapshot metadata, and governance mapping satisfy transition rules
- **THEN** governance assertions pass using stable invariant IDs without relying on phase-title literals

#### Scenario: Missing transition invariant metadata fails deterministically
- **WHEN** any required transition invariant field or mapping is absent
- **THEN** governance fails with deterministic diagnostics including invariant id, artifact path, and expected-vs-actual summary
