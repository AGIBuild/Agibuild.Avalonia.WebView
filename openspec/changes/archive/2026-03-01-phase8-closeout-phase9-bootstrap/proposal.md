## Why

Phase 8 (Bridge V2 & Platform Parity) has all 8 functional milestones (M8.1–M8.8) completed and archived, with M8.9 (Platform Feature Parity) implemented and documented. The phase needs formal closeout — transition markers updated, evidence snapshot refreshed, and ROADMAP moved to Phase 9.

Phase 9 targets **1.0 GA Release Readiness**: the project has achieved all core goals (G1–G4) and experience goals (E1–E3) across 21 preview releases, but remains in `0.1.x-preview`. Stabilizing the API surface, publishing the npm bridge package, re-baselining performance, and producing a structured changelog are the remaining steps to a credible 1.0 stable release.

## What Changes

- **Close Phase 8**: Mark M8.9 as Done, update `phase8-bridge-v2-parity` transition markers to completed, generate final closeout snapshot evidence.
- **Bootstrap Phase 9 in ROADMAP**: Define Phase 9 milestones (API freeze, npm publish, perf re-baseline, changelog, migration guide, stable release gate) and update transition markers to `phase9-ga-release-readiness`.
- **Update governance assertions**: Transition gate governance tests must reflect the new completed/active phase IDs.
- **Refresh evidence counts**: Update test counts and coverage figures in ROADMAP, PROJECT.md, and related docs after Phase 8 closeout.

## Capabilities

### New Capabilities

- `roadmap-phase-9-ga-release-readiness`: Phase 9 definition, milestones, and exit criteria for 1.0 stable release.

### Modified Capabilities

- `roadmap-phase-transition-management`: Transition markers updated from `phase8-bridge-v2-parity` (active) to `phase8-bridge-v2-parity` (completed) + `phase9-ga-release-readiness` (active).
- `continuous-transition-gate`: Governance assertions updated to enforce the new completed/active phase pair.
- `ci-evidence-contract-v2`: Closeout snapshot refreshed with Phase 8 final evidence.

## Non-goals

- Implementing any Phase 9 milestone deliverables (API freeze, npm publish, etc.) — this change only closes Phase 8 and defines Phase 9.
- Adding new runtime features or adapters.
- Changing any public API surface.

## Impact

- `openspec/ROADMAP.md` — Phase 8 status change + Phase 9 section added
- `openspec/PROJECT.md` — Evidence counts updated
- `build/Build.Governance.cs` — Transition gate phase IDs updated
- `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs` — Phase assertion constants updated
- `artifacts/test-results/closeout-snapshot.json` — Regenerated via `nuke ReleaseCloseoutSnapshot`
