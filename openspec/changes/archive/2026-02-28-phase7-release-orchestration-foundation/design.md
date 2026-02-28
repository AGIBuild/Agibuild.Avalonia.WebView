## Context

The repository now has stable transition governance (Phase 6), but release-readiness remains spread across separate checks (`Coverage`, `ValidatePackage`, strict OpenSpec validation, warning/dependency governance). Phase 7 requires a unified orchestration contract that converts these signals into deterministic release decisions and machine-readable diagnostics.

## Goals / Non-Goals

**Goals:**
- Define a single release-orchestration decision contract for CI publication workflows.
- Keep decision inputs explicit: CI evidence v2, governance gates, package validation, and versioning readiness.
- Ensure failure outputs are deterministic and machine-auditable for both humans and agents.

**Non-Goals:**
- No redesign of existing runtime/security contracts.
- No immediate refactor of all build targets in this artifact-only step.

## Decisions

### Decision 1: Add a dedicated capability spec instead of overloading existing ones
- **Chosen**: introduce `release-orchestration-gate` as a new capability, while using deltas for related existing specs.
- **Alternative**: place all requirements under `build-pipeline-resilience`.
  - Rejected: would mix orchestration semantics with low-level pipeline resilience concerns.

### Decision 2: Keep release decision payload inside CI evidence v2 contract
- **Chosen**: extend v2 evidence with decision summary and blocking reasons.
- **Alternative**: create a separate release-decision artifact schema.
  - Rejected: adds artifact proliferation and cross-file consistency burden.

### Decision 3: Enforce gate ordering in pipeline contract
- **Chosen**: require release-orchestration evaluation before publish actions.
- **Alternative**: allow publish attempts and rely on downstream rejection.
  - Rejected: violates fail-fast and determinism goals.

## Risks / Trade-offs

- **[Risk]** Over-constraining release workflow may slow emergency publication paths -> **Mitigation**: define explicit policy-governed override mechanism as future scoped requirement.
- **[Risk]** Decision payload schema drift across lanes -> **Mitigation**: lane-aware governance tests and shared assertion helpers.
- **[Trade-off]** More formal gates increase upfront complexity -> **Mitigation**: centralize diagnostics for lower long-term operational cost.

## Migration Plan

1. Finalize Phase 7 specs (new + deltas) and review requirement completeness.
2. Implement target wiring and evidence payload updates in build governance.
3. Add/adjust governance tests for release decision contract and publish-block behavior.
4. Validate with `nuke Test`, `nuke Coverage`, and strict OpenSpec checks.

## Open Questions

- Whether emergency release override should be in Phase 7 baseline or follow-up hardening change.
- Whether release decision payload should include optional non-blocking advisory signals in v2 scope.
