## Why

ROADMAP M7.3 requires adoption-oriented evidence, but current release decisions mainly focus on governance, packaging, and coverage readiness. To support framework adoption confidence, CI must emit deterministic adoption-readiness signals for docs, templates, and runtime critical paths, and wire them into release evidence.

## What Changes

- Define a formal adoption-readiness capability with machine-checkable KPI-style evidence contracts.
- Add structured adoption evidence to release artifacts so teams can audit readiness without manual log inspection.
- Introduce deterministic policy for adoption findings (blocking vs advisory) and preserve taxonomy-based diagnostics.
- Align template/docs/runtime readiness checks with release orchestration evidence flow.

## Capabilities

### New Capabilities
- `adoption-readiness-signals`: Define deterministic adoption KPI evidence for docs freshness, template operability, and runtime critical-path confidence.

### Modified Capabilities
- `ci-evidence-contract-v2`: Add structured adoption-readiness section to v2 evidence payloads.
- `release-orchestration-gate`: Define how adoption-readiness outputs are evaluated and surfaced in release decision context.
- `build-pipeline-resilience`: Add/compose governed adoption-readiness report generation in CI lane flow.

## Impact

- Affected specs: `ci-evidence-contract-v2`, `release-orchestration-gate`, `build-pipeline-resilience`, plus new `adoption-readiness-signals`.
- Affected code areas (expected): CI evidence emission in build targets, release decision report payload extensions, governance tests for evidence schema and policy semantics.
- Affected artifacts: adoption readiness report file(s), `closeout-snapshot.json`, release orchestration decision report.

## Non-goals

- Rewriting existing template systems or changing frontend sample architecture.
- Creating marketing/analytics telemetry pipelines outside CI evidence governance.
- Expanding adoption KPIs into product usage telemetry collection in this phase.
