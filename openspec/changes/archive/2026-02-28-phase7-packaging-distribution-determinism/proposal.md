## Why

Phase 7 is now active, but packaging and distribution readiness still depends on partially implicit checks across multiple targets and reports. To satisfy ROADMAP M7.2 with deterministic publish quality, release packaging policy must be explicit, machine-checkable, and consistently enforced before publish side effects.

## What Changes

- Define a deterministic distribution-readiness contract for release packaging outputs, metadata quality, and changelog freshness.
- Add machine-readable packaging/distribution governance evidence that can be consumed by release orchestration decisions.
- Tighten stable vs preview publication policy so stable release paths fail fast on any distribution-readiness gap.
- Preserve existing release-orchestration taxonomy while extending actionable source mapping for packaging faults.

## Capabilities

### New Capabilities
- `release-distribution-determinism`: Govern canonical package set, package metadata quality, and changelog/readiness evidence as deterministic publish prerequisites.

### Modified Capabilities
- `build-pipeline-resilience`: Extend governed pipeline behavior to emit distribution-readiness artifacts and deterministic diagnostics for packaging defects.
- `ci-evidence-contract-v2`: Include structured distribution-readiness summary fields in release evidence payloads for machine audit.
- `release-versioning-strategy`: Clarify stable/preview publication gates against explicit distribution-readiness outcomes.
- `release-orchestration-gate`: Consume distribution-readiness evidence as first-class blocking inputs for publish decisions.

## Impact

- Affected specs: `build-pipeline-resilience`, `ci-evidence-contract-v2`, `release-versioning-strategy`, `release-orchestration-gate`, plus new `release-distribution-determinism`.
- Affected code areas (expected): `build/Build.Governance.cs`, `build/Build.Packaging.cs`, release evidence/report generation, governance tests.
- Affected artifacts: `artifacts/test-results/*distribution*`, `closeout-snapshot.json`, release decision report payload.

## Non-goals

- Redesigning package identity strategy or changing canonical package naming conventions.
- Introducing new publish infrastructure providers or changing repository release topology.
- Refactoring unrelated runtime/bridge features outside packaging and release evidence governance scope.
