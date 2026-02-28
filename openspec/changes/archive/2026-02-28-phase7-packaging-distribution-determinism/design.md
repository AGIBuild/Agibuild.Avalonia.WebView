## Context

ROADMAP Phase 7 M7.2 requires deterministic packaging and distribution quality gates before publish side effects. Current release orchestration already blocks on governance and quality thresholds, but distribution readiness signals (canonical package set completeness, metadata policy completeness, changelog readiness mapping) are still partially implicit and spread across multiple targets.

This design aligns with PROJECT goals **G3 (Secure by Default)** and **G4 (Contract-Driven Testability)** by making release distribution policy explicit, machine-checkable, and test-governed.

## Goals / Non-Goals

**Goals:**
- Add a deterministic distribution-readiness contract consumed by release orchestration.
- Emit machine-readable distribution readiness evidence for CI and release audit.
- Preserve stable blocking taxonomy and deterministic expected-vs-actual diagnostics.
- Keep lane behavior deterministic across `Ci` and `CiPublish` for distribution-relevant evidence.

**Non-Goals:**
- Replacing MinVer/tag semantics or changing package naming strategy.
- Introducing new external publish backends.
- Refactoring unrelated runtime bridge or adapter behavior.

## Decisions

### Decision 1: Introduce a dedicated `release-distribution-determinism` capability
- **Why:** Packaging/distribution policy deserves explicit normative requirements instead of being embedded only in target logic.
- **Alternative considered:** Continue extending `build-pipeline-resilience` only.
- **Trade-off:** Slightly more spec surface, but clearer governance ownership and traceability.

### Decision 2: Treat distribution evidence as first-class release decision input
- **Why:** `ReleaseOrchestrationGovernance` should evaluate explicit artifacts rather than infer distribution state from side effects.
- **Alternative considered:** Keep only inline checks inside release gate.
- **Trade-off:** One more report artifact, but improved auditability and deterministic triage.

### Decision 3: Keep stable/preview policy split explicit in spec and diagnostics
- **Why:** Stable publication has stronger quality expectations than preview publication.
- **Alternative considered:** Single policy path with optional flags.
- **Trade-off:** More explicit branch logic, but less ambiguity for release operators.

### Decision 4: Prefer additive requirement deltas for existing capabilities
- **Why:** Existing requirements remain valid; M7.2 extends policy depth.
- **Alternative considered:** Large MODIFIED replacements for existing requirements.
- **Trade-off:** More requirement entries, but lower regression risk in semantic interpretation.

## Risks / Trade-offs

- **[Risk] Distribution gate over-constrains preview releases** → **Mitigation:** Separate stable blocking semantics from preview advisory semantics in requirements and tests.
- **[Risk] Artifact drift between reports and closeout snapshot** → **Mitigation:** Require explicit evidence-linkage fields in `ci-evidence-contract-v2`.
- **[Risk] Taxonomy ambiguity for package defects** → **Mitigation:** Require stable categories and expected-vs-actual schema in decision payload.
- **[Risk] Cross-lane inconsistency** → **Mitigation:** Add governance assertions for lane-consistent producer/consumer evidence paths.

## Testing Strategy

- Add/extend governance unit tests for:
  - distribution-readiness report contract schema;
  - release decision blocking reason mapping for package metadata and distribution defects;
  - stable vs preview policy branch behavior.
- Validate artifact-level contract via `openspec validate --all --strict`.
- Run build-level verification using `nuke Test`, `nuke Coverage`, and `nuke ReleaseOrchestrationGovernance`.

## Migration Plan

1. Add specs for new capability and modified capability deltas.
2. Implement report emission and release-decision consumption.
3. Add governance tests and update lane dependency assertions.
4. Run strict/spec/build validation baseline before archive.

## Open Questions

- Should changelog readiness be blocking for preview lanes or advisory-only until stable tag context is present?
- Do we require a single consolidated distribution report artifact or multiple specialized report artifacts with a top-level index?
