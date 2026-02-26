## Why

Phase 5 delivered web-first hybrid core capabilities, but release confidence is still limited by governance gaps in branch coverage, dependency risk scanning, platform parity evidence freshness, and performance regression visibility.  
This change advances ROADMAP Phase 3.3 "Performance & Quality" and reinforces G3/G4 by making quality signals deterministic and machine-enforced in CI.

## What Changes

- Add branch-coverage governance to the build pipeline, with explicit threshold enforcement in `Coverage` and closeout snapshot evidence.
- Add deterministic dependency vulnerability governance (NuGet + npm) as a CI gate with machine-readable reports.
- Strengthen compatibility matrix freshness governance so platform-coverage evidence remains aligned with executable automation manifests.
- Add benchmark-baseline governance that detects performance regressions from recorded benchmark evidence.
- Tighten DevTools stability governance for macOS host path by introducing deterministic contract checks around open/close state transitions.

## Capabilities

### New Capabilities

- `dependency-security-governance`: Define CI requirements for dependency vulnerability scanning and fail-fast policy.

### Modified Capabilities

- `build-pipeline-resilience`: Add branch coverage threshold and dependency governance integration in CI gates.
- `webview-compatibility-matrix`: Add freshness and parity requirements that tie matrix rows to executable evidence manifests.
- `performance-benchmarks`: Add baseline-regression governance requirements for benchmark outputs.
- `devtools-toggle`: Add deterministic runtime stability requirements for macOS DevTools open/close transitions.

## Non-goals

- Raising branch coverage target to a new hard number in this change; this change introduces governance rails, not immediate large-scale test rewrites.
- Re-architecting platform adapters or changing public API surface.
- Introducing fallback compatibility paths for legacy behavior.

## Impact

- Build orchestration (`build/Build*.cs`) and governance tests (`tests/Agibuild.Fulora.UnitTests/*Governance*`).
- CI evidence artifacts under `artifacts/test-results`.
- Compatibility/benchmark governance metadata and documentation contracts.
