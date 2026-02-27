## ADDED Requirements

### Requirement: Evidence v2 closeout snapshots SHALL include phase-neutral transition metadata
Closeout snapshot artifacts governed by CI evidence contract v2 MUST include explicit transition metadata fields describing `completedPhase` and `activePhase` as normalized phase identifiers, independent of hardcoded phase numbers in artifact names.

#### Scenario: Transition metadata is present and normalized
- **WHEN** governance validates a closeout snapshot artifact
- **THEN** `completedPhase` and `activePhase` fields are present, non-empty, and normalized for machine comparison

#### Scenario: Missing transition metadata fails gate
- **WHEN** either transition metadata field is missing, empty, or non-normalized
- **THEN** governance fails before release-readiness sign-off with deterministic diagnostics
