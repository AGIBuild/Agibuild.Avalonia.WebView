## Why

M4.5 completed shell DX bootstrap, but Phase 4 still needs production-readiness proof under sustained shell workloads. M4.6 is required to harden long-run attach/detach and multi-window cycles, and to refresh shell compatibility evidence with machine-checkable traceability (ROADMAP Deliverable 4.6, aligned with G4).

## What Changes

- Add long-run shell production validation integration coverage focused on repeated attach/detach, policy routing, host capability execution, and managed-window teardown invariants.
- Introduce a machine-readable shell compatibility/production matrix artifact with explicit platform coverage and executable evidence mapping.
- Extend runtime-critical-path governance to include shell soak scenarios as release-critical checks.
- Extend governance tests to fail fast when shell matrix evidence drifts or referenced tests are missing.

## Non-goals

- Introducing new shell feature domains beyond M4.1–M4.5 scope.
- Building a platform-specific benchmark harness or perf profiler.
- Adding compatibility fallbacks for legacy design paths.

## Capabilities

### New Capabilities
- `shell-production-validation`: Defines long-run shell soak validation and machine-readable production-readiness matrix requirements.

### Modified Capabilities
- None.

## Impact

- **Roadmap alignment**: Implements **Phase 4 M4.6 / Deliverable 4.6** (`Shell stress/soak lane + release-readiness checklist`).
- **Goal alignment**: Advances **G4** through deterministic, auditable shell hardening evidence; supports **G3** by validating permission/capability governance under stress.
- **Affected systems**: automation integration tests, runtime critical-path manifest, governance unit tests, and new shell production matrix artifact.
