## Context

`release-1-0-preparation` completed versioning/readme/package metadata governance, but CI still lacks deterministic controls for branch-coverage regression, dependency risk drift, and benchmark parity freshness.  
This change focuses on ROADMAP Phase 3.3 quality hardening while preserving current runtime architecture and public contracts.

## Goals

- Enforce branch coverage as a first-class CI gate alongside line coverage.
- Fail fast on known dependency vulnerabilities in both NuGet and npm ecosystems.
- Keep compatibility/performance evidence machine-checkable and fresh.
- Stabilize DevTools toggle behavior expectations for macOS path in governance tests.

## Non-Goals

- Large refactors in adapters/runtime.
- New fallback paths for legacy behavior.
- Immediate broad increase of branch coverage to a high target by test rewrites.

## Decisions

### D1. Branch coverage gate in `Coverage` target

- Add `BranchCoverageThreshold` parameter to `BuildTask`.
- Parse Cobertura `branch-rate` from merged report.
- Fail `Coverage` when branch coverage falls below threshold.
- Emit both line and branch coverage into GitHub step summary and closeout snapshot payload.

Rationale: Branch coverage is currently observable but not governed; adding a hard gate prevents silent regression.

### D2. Dependency vulnerability governance target

- Add target `DependencyVulnerabilityGovernance` in build governance partial.
- Execute:
  - `dotnet list <solution> package --vulnerable --include-transitive`
  - npm audit against template/sample web workspaces that include lock files.
- Produce machine-readable report under `artifacts/test-results/dependency-governance-report.json`.
- Wire target into `Ci` and `CiPublish` dependency graph.

Rationale: Security posture must be deterministic and release-blocking for known vulnerabilities.

### D3. Compatibility matrix freshness checks

- Extend governance tests to assert matrix platform set and capability evidence remain aligned with runtime automation manifests.
- Add explicit assertion that newly introduced capabilities cannot exist in matrix without executable evidence references.

Rationale: Matrix drift weakens confidence claims and can mislead release readiness decisions.

### D4. Benchmark baseline regression check

- Add a lightweight benchmark governance check that verifies benchmark baseline artifact presence and compares against configured tolerance for key metrics.
- Keep execution deterministic by using pre-generated benchmark summary artifacts in CI lanes where full benchmark run is too expensive.

Rationale: Performance regressions should be detected by policy, not manual inspection.

### D5. DevTools stability governance for macOS semantics

- Add deterministic contract tests around open/close state transitions and idempotence expectations shared across platform adapters, including macOS branch.
- Validate no-op/unsupported paths remain explicit and stable.

Rationale: DevTools behavior is part of DX promises and must not regress silently.

## Alternatives Considered

### A1. Raise branch threshold directly to 90+

Rejected for this change: introduces broad test rewrite pressure and slows near-term governance rollout. We gate current baseline first, then raise in a follow-up change with targeted tests.

### A2. Full benchmark execution on every CI run

Rejected for this change: high runtime/cost and flakiness risk. We use deterministic evidence artifact checks plus optional dedicated lane for full benchmark runs.

## Rollout

1. Implement governance gates and tests.
2. Run `nuke Coverage`, targeted unit tests, and `openspec validate --all --strict`.
3. Enable CI gating by wiring new targets into `Ci`/`CiPublish`.

## Risks & Mitigations

- **Risk:** False positives from dependency scanners due to feed/network issues.  
  **Mitigation:** Distinguish scanner failure vs vulnerability findings in report, but still fail closed for governance lanes.
- **Risk:** Branch coverage threshold breaks current baseline unexpectedly.  
  **Mitigation:** Start from measured baseline threshold and encode as explicit parameter.
