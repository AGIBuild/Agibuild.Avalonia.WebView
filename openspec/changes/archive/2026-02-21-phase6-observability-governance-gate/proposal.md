## Why

OpenSpec strict validation is currently green but not enforced as a first-class build gate, so regressions can re-enter CI silently through later pipeline edits. We need to make strict validation and diagnostic-schema evolution governance deterministic in CI to sustain Phase 5 production governance outcomes and Phase 6 observability continuity.

## What Changes

- Add a dedicated build governance target that runs `openspec validate --all --strict` and fails deterministically on violations.
- Wire the new governance target into `Ci` and `CiPublish` target dependencies so all primary CI/release pipelines enforce it.
- Add governance tests that lock this gate in build orchestration.
- Add explicit diagnostic-schema evolution governance requirements so version progression remains auditable and cross-lane consistent.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `build-pipeline-resilience`: CI and release targets enforce repository-wide OpenSpec strict validation as a mandatory gate.
- `observability-diagnostic-schema-versioning`: diagnostic schema version evolution is governed through shared expectations across lanes and CI gate continuity.

## Non-goals

- Introduce new runtime diagnostic payload fields (already delivered in prior change).
- Add fallback/legacy validation modes for non-strict spec formats.
- Rework non-governance build targets unrelated to CI quality gates.

## Impact

- Build pipeline: `build/Build.cs` target graph and governance execution flow.
- Governance tests: source-level contract checks in `AutomationLaneGovernanceTests`.
- OpenSpec quality posture: strict validation becomes a hard CI/release invariant, reducing drift risk.
