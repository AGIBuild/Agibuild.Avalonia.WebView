## ADDED Requirements

### Requirement: Evidence v2 closeout transition metadata SHALL be lane-provenance consistent
Closeout snapshot artifacts governed by CI evidence contract v2 MUST ensure transition metadata (`completedPhase`, `activePhase`) is consistent with provenance lane context and producer target semantics.

#### Scenario: Lane-provenance consistency passes
- **WHEN** closeout snapshot v2 metadata is validated
- **THEN** transition metadata and provenance fields are mutually consistent for the producing lane context

#### Scenario: Lane-provenance inconsistency fails gate
- **WHEN** transition metadata conflicts with provenance lane context or producer target semantics
- **THEN** evidence governance fails deterministically with expected-vs-actual diagnostics

### Requirement: Evidence v2 transition metadata SHALL match roadmap machine-checkable phase state
Closeout snapshot v2 transition fields MUST match the repository roadmap machine-checkable transition section for completed and active phase identifiers.

#### Scenario: Snapshot phase ids match roadmap state
- **WHEN** governance compares closeout snapshot transition metadata with roadmap machine-checkable transition state
- **THEN** evidence validation passes

#### Scenario: Snapshot phase ids drift from roadmap state
- **WHEN** completed or active phase identifiers in the snapshot diverge from roadmap machine-checkable transition state
- **THEN** evidence governance fails before release-readiness sign-off
