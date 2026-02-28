## Why

Roadmap machine-checkable phase markers and governance closeout constants have drifted from the archived change history. This creates audit ambiguity for release evidence and risks enforcing an outdated transition baseline (`phase6 -> phase7`) after Phase 7 closeout work is complete.

This should be fixed now to keep `G4` (contract-driven testability/auditability) and ROADMAP transition governance deterministic before the next implementation cycle.

## What Changes

- Reconcile roadmap transition state to the next adjacent baseline after Phase 7 closeout (`completedPhase`/`activePhase` pair moves together).
- Refresh `ROADMAP.md` phase status narrative and closeout evidence mapping so completed-phase references match archived artifacts.
- Update build governance closeout snapshot constants and completed-phase archive mapping to the same baseline.
- Update governance tests that assert roadmap-transition continuity and closeout mapping invariants.
- Re-run strict OpenSpec and governance verification commands to ensure no drift remains.

## Capabilities

### New Capabilities
- `phase-baseline-reconciliation`: Defines deterministic repository-level phase baseline reconciliation between roadmap markers and governance closeout constants.

### Modified Capabilities
- `roadmap-phase-transition-management`: Tighten requirement that roadmap closeout mapping and release closeout snapshot baseline are synchronized as one governed unit.
- `continuous-transition-gate`: Clarify stale-baseline detection expectations when repository transition metadata is refreshed.

## Impact

- Affected docs: `openspec/ROADMAP.md`.
- Affected governance implementation: `build/Build.Governance.cs`.
- Affected tests: `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs`.
- Affected specs: new `phase-baseline-reconciliation`, plus deltas for `roadmap-phase-transition-management` and `continuous-transition-gate`.

## Non-goals

- Introducing new runtime/bridge/adapter features.
- Changing release taxonomy structure or evidence schema version.
- Archiving phases or publishing packages as part of this change.
