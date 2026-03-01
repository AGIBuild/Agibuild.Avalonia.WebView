## 1. ROADMAP Phase 8 closeout

- [x] 1.1 Update ROADMAP.md Phase 8 header from `(🚧 Active)` to `(✅ Completed)`, mark M8.9 as Done (Deliverable: roadmap-phase-transition-management; Acceptance: Phase 8 header shows completed status).
- [x] 1.2 Add Phase 8 closeout evidence section with archived change IDs covering M8.1–M8.9 (Deliverable: roadmap-phase-transition-management; Acceptance: ROADMAP lists all Phase 8 archive change IDs).

## 2. ROADMAP Phase 9 bootstrap

- [x] 2.1 Add Phase 9 section to ROADMAP.md with milestones M9.1–M9.7 (API freeze, npm publish, perf re-baseline, changelog, migration guide, stable release gate) (Deliverable: roadmap-phase-9-ga-release-readiness; Acceptance: Phase 9 section exists with milestone table and exit criteria).
- [x] 2.2 Update Phase Transition Status markers: completed → `phase8-bridge-v2-parity`, active → `phase9-ga-release-readiness` (Deliverable: roadmap-phase-transition-management; Acceptance: machine-checkable markers match new phase pair).
- [x] 2.3 Update dependency graph in ROADMAP to show Phase 8 (✅ Completed) → Phase 9 (🚧 Active) (Deliverable: roadmap-phase-9-ga-release-readiness; Acceptance: dependency chain includes Phase 9).
- [x] 2.4 Update Phase Overview ASCII diagram to include Phase 9 column (Deliverable: roadmap-phase-9-ga-release-readiness; Acceptance: overview shows all 10 phases).

## 3. Governance transition gate update

- [x] 3.1 Update `Build.Governance.cs` `completedPhase` to `"phase8-bridge-v2-parity"` and `activePhase` to `"phase9-ga-release-readiness"` (Deliverable: continuous-transition-gate; Acceptance: `ReleaseCloseoutSnapshot` target uses new constants).
- [x] 3.2 Update `Build.Governance.cs` `completedPhaseCloseoutChangeIds` to Phase 8 archive change IDs (Deliverable: continuous-transition-gate; Acceptance: closeout change IDs match Phase 8 archives).
- [x] 3.3 Update `AutomationLaneGovernanceTests.cs` phase marker assertions to `phase8-bridge-v2-parity` (completed) and `phase9-ga-release-readiness` (active) (Deliverable: continuous-transition-gate; Acceptance: governance unit test passes with new markers).
- [x] 3.4 Update `AutomationLaneGovernanceTests.cs` `completedPhaseCloseoutChangeIds` to Phase 8 archive change IDs (Deliverable: continuous-transition-gate; Acceptance: unit test validates Phase 8 archives in ROADMAP).

## 4. Evidence refresh and validation

- [x] 4.1 Run `nuke ReleaseOrchestrationGovernance` and verify all targets pass (Deliverable: ci-evidence-contract-v2; Acceptance: all governance targets succeed with updated transition markers).
- [x] 4.2 Run `openspec validate --all --strict` and verify all specs pass (Deliverable: ci-evidence-contract-v2; Acceptance: 0 failures).
