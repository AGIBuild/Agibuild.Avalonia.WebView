## Context

Phase 8 (Bridge V2 & Platform Parity) milestones M8.1–M8.9 are all functionally complete with archived OpenSpec changes. The project has accumulated 1113 unit tests + 180 integration tests at 95%+ line coverage across 21 preview releases (v0.1.21-preview). All core goals (G1–G4) and experience goals (E1–E3) are achieved.

The governance system enforces phase transition markers via two independent mechanisms:
1. **`Build.Governance.cs` → `ReleaseCloseoutSnapshot` target**: Hardcoded `completedPhase` / `activePhase` constants that feed the transition gate.
2. **`AutomationLaneGovernanceTests.cs` → `Phase_transition_roadmap_and_shell_governance_artifacts_remain_consistent`**: Asserts ROADMAP contains specific completed/active phase markers and closeout change IDs.

Both must be updated atomically to avoid CI failures.

## Goals / Non-Goals

**Goals:**
- Formally close Phase 8 by updating all transition markers and evidence artifacts
- Define Phase 9 (GA Release Readiness) milestones and exit criteria in ROADMAP
- Keep governance pipeline green across the transition
- Refresh evidence counts (test totals, coverage, version) in documentation

**Non-Goals:**
- Implementing any Phase 9 deliverable (API freeze, npm publish, etc.)
- Modifying any runtime code or public API surface
- Adding new tests beyond governance assertion updates

## Decisions

### D1: Phase transition marker update strategy

**Choice**: Update both `Build.Governance.cs` and `AutomationLaneGovernanceTests.cs` in the same commit.

**Rationale**: The governance pipeline (nuke targets + unit tests) validates transition marker consistency. If only one is updated, `nuke Test` fails. Atomic update is the only safe path — this is a pattern established in the Phase 6→7 and Phase 7→8 transitions.

**Specifics**:
- `Build.Governance.cs`: `completedPhase` → `"phase8-bridge-v2-parity"`, `activePhase` → `"phase9-ga-release-readiness"`
- `Build.Governance.cs`: `completedPhaseCloseoutChangeIds` → Phase 8 archive change IDs
- `AutomationLaneGovernanceTests.cs`: Update `Completed phase id` and `Active phase id` assertions
- `AutomationLaneGovernanceTests.cs`: Update `completedPhaseCloseoutChangeIds` array to Phase 8 archives
- `ROADMAP.md`: Phase 8 header → `(✅ Completed)`, Phase 9 header → `(🚧 Active)`, transition markers updated

### D2: Phase 9 milestone structure

**Choice**: Seven milestones covering the path from preview to 1.0 stable.

| Milestone | Focus |
|---|---|
| M9.1 Phase 8 Evidence Closeout | Final closeout snapshot with Phase 8 evidence |
| M9.2 API Surface Freeze | Breaking change audit, semver 1.0.0 commitment |
| M9.3 npm Bridge Publication | `@agibuild/bridge` published to npm registry |
| M9.4 Performance Re-baseline | Updated benchmarks after Phase 8 changes |
| M9.5 Changelog & Release Notes | Structured changelog from v0.1.0 to v1.0.0 |
| M9.6 Migration Guide | Electron/Tauri → Fulora migration documentation |
| M9.7 Stable Release Gate | 1.0.0 stable NuGet + npm publish |

**Rationale**: Each milestone is independently deliverable and testable. The ordering reflects natural dependencies — you can't freeze the API without auditing it, and you can't do a stable release without the changelog.

### D3: ROADMAP Phase 8 closeout evidence

**Choice**: Include Phase 8 archive change IDs as closeout evidence, matching the pattern from Phase 7.

Phase 8 closeout change IDs (from `openspec/changes/archive/`):
- `2026-02-28-bridge-cancellation-token-support`
- `2026-02-28-bridge-async-enumerable-streaming`
- `2026-02-28-bridge-generics-overloads`
- `2026-03-01-phase9-functional-triple-track`
- `2026-03-01-deep-link-native-registration`
- `2026-02-28-bridge-diagnostics-safety-net`
- `2026-02-28-platform-feature-parity`
- `2026-02-28-phase7-closeout-phase8-reconciliation`

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| Governance test failure if markers updated partially | Atomic commit for all marker files |
| Phase 9 milestone scope too broad for single phase | Each milestone is independently scoped; can be split further if needed |
| Closeout snapshot regeneration may show different coverage numbers | Run `nuke ReleaseCloseoutSnapshot` after all changes and capture actual numbers |

## Testing Strategy

- **CT**: `AutomationLaneGovernanceTests.Phase_transition_roadmap_and_shell_governance_artifacts_remain_consistent` must pass with new phase IDs
- **Governance**: `nuke ReleaseOrchestrationGovernance` must pass (includes `ContinuousTransitionGateGovernance`)
- **Validation**: `openspec validate --all --strict` must pass after ROADMAP updates
