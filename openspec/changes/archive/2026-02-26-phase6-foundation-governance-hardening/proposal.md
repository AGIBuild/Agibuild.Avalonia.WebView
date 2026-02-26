## Why

Phase 5 delivery is complete, but governance confidence still depends heavily on brittle string-contains assertions and loosely coupled evidence files. As we move into Phase 6, we need a more semantic and machine-verifiable governance baseline to prevent silent CI drift and reduce maintenance cost when build/test orchestration evolves.

## What Changes

- Formalize a Phase 6 foundation slice focused on governance hardening and evidence determinism.
- Replace fragile governance assertions with semantic validations over structured sources (JSON artifacts, target dependency graph shape, and capability mapping invariants).
- Define and adopt a versioned release evidence contract (v2) for `CiPublish` outputs, including provenance metadata and lane lineage.
- Extend bridge package distribution governance with package-manager parity (npm/pnpm/yarn) and Node LTS compatibility checks.

## Capabilities

### New Capabilities
- `governance-semantic-assertions`: Semantic, machine-checkable governance assertions that reduce false positives from textual coupling.
- `ci-evidence-contract-v2`: Versioned release evidence schema with deterministic provenance fields and release-lane consistency rules.

### Modified Capabilities
- `build-pipeline-resilience`: Release-pipeline governance now validates evidence-contract v2 and semantic gate continuity.
- `runtime-automation-validation`: Runtime critical-path evidence requirements align to v2 schema and provenance invariants.
- `shell-production-validation`: Shell matrix/runtime manifest consistency checks align to shared semantic assertion rules.
- `bridge-npm-distribution`: Distribution governance expands from basic package presence to multi-package-manager and Node LTS parity checks.

## Non-goals

- Introducing new runtime product features unrelated to governance/evidence quality.
- Reworking adapter architecture, bridge RPC semantics, or capability policy execution order.
- Adding backward-compatibility fallback branches for legacy evidence formats.
- Adding migration tracks or dual-write compatibility for legacy evidence/governance paths.

## Impact

- Governance tests: `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs` and shared testing helpers.
- Build orchestration and reports: `build/Build*.cs` governance targets and emitted `CiPublish` artifacts.
- Evidence artifacts in `tests/*.json` that serve as machine-checkable release inputs.
- Bridge package validation flow and release-lane matrix coverage for `packages/bridge`.
