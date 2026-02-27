## Context

This change implements Roadmap **Phase 6 / M6.3 Continuous Transition Gate** on top of M6.1+M6.2 outputs already present in CI governance (`ReleaseCloseoutSnapshot`, v2 provenance, transition invariant IDs).  
Current governance validates many transition fields, but CI lane continuity can still drift when `Ci` and `CiPublish` dependency graphs diverge or when transition metadata checks are not evaluated with lane-context parity.  
The architecture must remain consistent with contract-first governance in `build/*` and semantic assertions in `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs`, aligned with design principles in `docs/agibuild_webview_design_doc.md` (contract-first, deterministic behavior, testability).

## Goals / Non-Goals

**Goals:**
- Enforce deterministic transition-gate parity between `Ci` and `CiPublish` for closeout-critical governance targets.
- Make transition continuity checks machine-readable and lane-aware (invariant ID + lane + expected/actual).
- Ensure closeout snapshot transition metadata remains semantically consistent with roadmap machine-checkable status fields.
- Preserve CI determinism and strengthen governance diagnostics (Goal alignment: **G3**, **G4**).

**Non-Goals:**
- No new runtime WebView features, platform adapter semantics, or bridge API changes.
- No restructuring of archived OpenSpec changes or historical snapshots.
- No replacement of existing evidence contract versioning model beyond M6.3 continuity constraints.

## Decisions

### 1) Add a dedicated capability spec for transition-gate continuity
- **Decision**: Introduce new capability `continuous-transition-gate` instead of only extending unrelated specs.
- **Why**: M6.3 has distinct semantics (lane parity + continuity) and benefits from explicit contract ownership.
- **Alternatives considered**:
  - Extend only `build-pipeline-resilience`: rejected, would mix orthogonal concerns and reduce traceability.
  - Extend only `governance-semantic-assertions`: rejected, lacks CI target-graph contract coverage.

### 2) Keep enforcement split: build graph checks in build specs, semantic checks in governance specs
- **Decision**:  
  - CI target parity constraints stay in `build-pipeline-resilience`;  
  - provenance/transition continuity stays in `ci-evidence-contract-v2`;  
  - deterministic diagnostics stay in `governance-semantic-assertions`.
- **Why**: Matches existing layered architecture (`build` orchestration vs semantic assertion tests).
- **Alternatives considered**:
  - Centralize all rules in one spec: rejected due to weak maintainability and ambiguous ownership.

### 3) Implement lane-aware deterministic diagnostics as first-class output
- **Decision**: Every M6.3 failure path SHALL include invariant ID, lane context, artifact path, and expected-vs-actual summary.
- **Why**: Required for CI agent resolution and stable regression triage.
- **Alternatives considered**:
  - Human-readable-only errors: rejected because they are fragile and hard to automate.

### 4) Testing strategy anchored in contract/governance tests
- **Decision**: Cover M6.3 primarily through governance CT/unit tests plus build target graph checks; no new runtime IT lane is required.
- **Coverage plan**:
  - Extend `AutomationLaneGovernanceTests` for lane parity and transition continuity invariants.
  - Validate build graph dependencies in `build/Build.cs` and closeout payload semantics in `build/Build.Governance.cs`.
  - Run `nuke Test`, `nuke Coverage`, and `openspec validate --all --strict`.
- **Rationale**: M6.3 is governance-contract work and should remain deterministic without browser/runtime nondeterminism.

## Risks / Trade-offs

- **[Risk] False-positive gate failures during target refactoring** → **Mitigation**: bind checks to invariant IDs and explicit expected dependency sets, not brittle source snippets.
- **[Risk] CI lane-specific differences become over-constrained** → **Mitigation**: enforce parity only for closeout-critical targets; keep lane-specific targets explicitly scoped.
- **[Risk] Spec drift between new and existing capabilities** → **Mitigation**: update modified capability specs in the same change and require strict OpenSpec validation.
