## Context

Coverage and test gates already run in CI, but closeout evidence is manually copied to roadmap or archived evidence files. A small automation layer can standardize the source-of-truth payload and reduce human error.

## Goals / Non-Goals

**Goals:**
- Generate one machine-readable closeout snapshot from latest test and coverage artifacts.
- Keep generation deterministic and local to build pipeline.
- Wire target into CI/release orchestration.

**Non-Goals:**
- No direct markdown rewrite automation.
- No historical time-series storage.
- No additional external dependencies.

## Decisions

### Decision 1: Snapshot as JSON artifact
- Output `phase5-closeout-snapshot.json` in `artifacts/test-results/`.
- Include test totals, coverage line rate, OpenSpec strict gate status hints, and source file paths.

### Decision 2: Parse existing artifacts, do not rerun gates
- Snapshot target reads TRX and Cobertura files produced by existing targets.
- Avoid duplicate test execution.

### Decision 3: Include in CI and CiPublish dependencies
- CI paths automatically produce fresh snapshot on every governed run.

## Risks / Trade-offs

- [Risk] Missing artifact files when target called standalone.  
  → Mitigation: enforce dependency order and fail with clear message.
- [Risk] TRX schema variations.  
  → Mitigation: parse stable aggregate fields and fail fast when absent.

## Testing Strategy

- Build target execution via `nuke Ci`/`nuke CiPublish` dependency chain.
- Unit/governance tests remain green.
- OpenSpec strict validation passes.
