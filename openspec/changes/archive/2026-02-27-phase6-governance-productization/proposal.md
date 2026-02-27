## Why

Phase 5 is complete, but core governance still encodes Phase 5-specific assumptions in build targets, evidence payload fields, and semantic tests. This blocks clean Phase 6 progression and creates repeat migration cost for each next phase. We need a phase-neutral governance contract now to keep G4 testability and G3 deterministic release gating sustainable.

## What Changes

- Formalize a Phase 6 governance productization slice that removes phase-number coupling from closeout governance artifacts.
- Replace phase-specific closeout naming/fields in governed release evidence with phase-neutral closeout contract semantics.
- Convert roadmap/governance consistency checks from "Phase 5 literal" assertions to invariant-driven semantic checks.
- Add explicit roadmap transition governance requirements so "completed previous phase + declared active next phase" is machine-checkable.
- Keep current runtime behavior unchanged; scope is governance contracts, evidence schema, and test semantics.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `build-pipeline-resilience`: closeout snapshot and CI governance become phase-neutral and reusable across future phases.
- `governance-semantic-assertions`: roadmap/build governance checks move from literal phase strings to stable invariant IDs and semantic assertions.
- `ci-evidence-contract-v2`: evidence v2 adds/uses phase-neutral closeout metadata requirements.
- `electron-replacement-foundation`: roadmap phase-governance requirements expand from Phase 5 closeout-only to continuous phase transition governance.

## Impact

- Build governance implementation in `build/Build.Governance.cs` and related target naming/payload contracts.
- CI target dependencies and governance reports in `build/Build.cs`.
- Governance tests in `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs`.
- OpenSpec artifacts: `openspec/ROADMAP.md` and modified capability specs for phase-transition consistency.

## Non-goals

- No new runtime product features or shell capability expansion.
- No compatibility fallback path for old phase-specific governance contract.
- No adapter, bridge protocol, or policy execution redesign.
