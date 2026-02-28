# continuous-transition-gate Specification

## Purpose
Define deterministic CI transition gate parity and closeout transition continuity checks across `Ci` and `CiPublish`, ensuring lane-aware invariant enforcement and machine-readable diagnostics for roadmap transition governance.
## Requirements
### Requirement: Closeout-critical transition gate parity SHALL be enforced across Ci and CiPublish
The governance pipeline MUST enforce deterministic parity for closeout-critical transition gates across `Ci` and `CiPublish` lane dependency graphs.
Parity SHALL be evaluated by invariant-defined target groups so lane-specific naming differences can be mapped explicitly without weakening enforcement.

#### Scenario: Transition gate parity passes
- **WHEN** `Ci` and `CiPublish` both include all required closeout-critical transition-gate groups per invariant mapping
- **THEN** governance parity checks pass for continuous transition gate enforcement

#### Scenario: Transition gate parity drift fails deterministically
- **WHEN** a closeout-critical transition-gate group exists in one lane but is missing or unmapped in the other lane
- **THEN** governance fails with deterministic diagnostics before release progression

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

### Requirement: Continuous transition gate failures SHALL emit lane-aware invariant diagnostics
Any continuous transition gate failure MUST emit machine-readable diagnostics including invariant id, lane context, artifact path, and expected-vs-actual transition-gate values.

#### Scenario: Failure diagnostic includes lane-aware fields
- **WHEN** a transition gate invariant is violated
- **THEN** emitted diagnostics include invariant id, lane context, artifact location, and expected-vs-actual values

