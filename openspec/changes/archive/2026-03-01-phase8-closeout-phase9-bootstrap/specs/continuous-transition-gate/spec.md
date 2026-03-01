## MODIFIED Requirements

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
