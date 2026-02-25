## Why

Phase closeout evidence currently depends on manual snapshot updates, which is error-prone and easy to drift from actual CI results. We need an automated, machine-readable snapshot generated from build artifacts.

## What Changes

- Add a build target that parses test and coverage artifacts to generate a Phase 5 closeout snapshot payload.
- Emit snapshot JSON into test-results artifacts for CI and audit consumption.
- Integrate snapshot generation into CI/release target chains.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `build-pipeline-resilience`: add deterministic closeout evidence snapshot generation from command outputs.
- `electron-replacement-foundation`: define closeout evidence snapshot automation expectation.

## Impact

- Affected build orchestration in `build/Build.cs`.
- No runtime product behavior changes.
- Reduces governance maintenance overhead and snapshot drift risk.

## Non-goals

- No automatic editing of roadmap/spec markdown files.
- No external reporting service integration.
- No release process redesign beyond adding artifact output.
