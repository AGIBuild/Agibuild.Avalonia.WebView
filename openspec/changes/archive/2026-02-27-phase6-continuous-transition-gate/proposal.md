## Why

Phase 6 is active and M6.3 requires continuous transition gating across `Ci` and `CiPublish`, but current checks still allow drift between lane dependency graphs and closeout transition assertions. This weakens deterministic governance and can let roadmap-transition regressions pass one lane while failing later in release flow.

## What Changes

- Add deterministic governance invariants to assert transition-gate parity between `Ci` and `CiPublish` for closeout-related targets and reports.
- Strengthen closeout snapshot validation to enforce stable `completedPhase`/`activePhase` transition semantics and producer-context consistency.
- Extend machine-readable governance diagnostics so failures identify lane, invariant id, artifact path, and expected-vs-actual transition metadata.
- Add/adjust governance and unit tests to prevent silent gate drift when adding or renaming CI targets.

## Capabilities

### New Capabilities
- `continuous-transition-gate`: Deterministic CI transition gate parity and closeout transition continuity checks across `Ci` and `CiPublish`.

### Modified Capabilities
- `build-pipeline-resilience`: Tighten CI target graph invariants for transition-gate parity and deterministic enforcement.
- `ci-evidence-contract-v2`: Clarify and enforce transition provenance continuity constraints for closeout snapshot validation.
- `governance-semantic-assertions`: Add lane-aware invariant diagnostics for transition-gate drift detection.

## Impact

- Affected code: `build/Build.cs`, `build/Build.Governance.cs`, governance helper paths under `tests/Agibuild.Fulora.Testing`, and `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs`.
- API impact: none for external runtime APIs; governance contract semantics become stricter.
- Dependency impact: none expected.
- Goal alignment: advances **G3** (secure/deterministic policy-governed pipeline behavior) and **G4** (contract-driven, machine-checkable governance testability).
- Roadmap alignment: directly implements **Phase 6 / M6.3 Continuous Transition Gate**, building on M6.1+M6.2 evidence-neutral transition model.

## Non-goals

- Introducing new runtime WebView features or platform adapter capabilities.
- Reworking historical phase archives or retrofitting prior release evidence payloads.
- Changing release channel semantics beyond transition-gate continuity requirements.
