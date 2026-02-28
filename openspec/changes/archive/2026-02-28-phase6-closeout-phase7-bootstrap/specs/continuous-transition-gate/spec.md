## MODIFIED Requirements

### Requirement: Transition metadata continuity SHALL remain phase-semantic and lane-consistent
Closeout transition governance MUST assert that `completedPhase` and `activePhase` metadata remain semantically consistent with machine-checkable roadmap transition state, MUST remain consistent with lane provenance context, and MUST be updated as an adjacent pair when roadmap baseline rolls to the next phase.

#### Scenario: Semantic continuity passes
- **WHEN** closeout snapshot transition metadata matches roadmap machine-checkable transition state and lane provenance context
- **THEN** transition continuity governance passes

#### Scenario: Semantic continuity mismatch fails
- **WHEN** snapshot transition metadata diverges from roadmap machine-checkable transition state or lane provenance context
- **THEN** governance fails with deterministic transition continuity diagnostics

#### Scenario: Non-adjacent or partial rollover is rejected
- **WHEN** completed/active phase metadata does not represent a single adjacent rollover pair from roadmap baseline
- **THEN** transition continuity governance fails before release evidence acceptance
