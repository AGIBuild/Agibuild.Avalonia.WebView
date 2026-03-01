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

### Requirement: Continuous transition gate enforces completed and active phase pair

The `ContinuousTransitionGateGovernance` target and `AutomationLaneGovernanceTests` SHALL enforce that the completed phase is `phase8-bridge-v2-parity` and the active phase is `phase9-ga-release-readiness`. Closeout change ID assertions SHALL reference Phase 8 archive entries.

#### Scenario: Build.Governance.cs constants reflect Phase 8 → Phase 9 transition

- **WHEN** the `ReleaseCloseoutSnapshot` target executes
- **THEN** the `completedPhase` constant SHALL be `"phase8-bridge-v2-parity"`
- **AND** the `activePhase` constant SHALL be `"phase9-ga-release-readiness"`
- **AND** the `completedPhaseCloseoutChangeIds` array SHALL contain Phase 8 archive change IDs

#### Scenario: Governance unit test asserts Phase 8 → Phase 9 markers in ROADMAP

- **WHEN** `Phase_transition_roadmap_and_shell_governance_artifacts_remain_consistent` executes
- **THEN** it SHALL assert ROADMAP contains `Completed phase id: \`phase8-bridge-v2-parity\``
- **AND** it SHALL assert ROADMAP contains `Active phase id: \`phase9-ga-release-readiness\``
- **AND** it SHALL assert ROADMAP contains Phase 8 closeout archive change IDs

### Requirement: Continuous transition gate failures SHALL emit lane-aware invariant diagnostics
Any continuous transition gate failure MUST emit machine-readable diagnostics including invariant id, lane context, artifact path, and expected-vs-actual transition-gate values.

#### Scenario: Failure diagnostic includes lane-aware fields
- **WHEN** a transition gate invariant is violated
- **THEN** emitted diagnostics include invariant id, lane context, artifact location, and expected-vs-actual values

