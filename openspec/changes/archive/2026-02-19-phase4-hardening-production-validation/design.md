## Context

- Phase 4 shell capabilities (M4.1–M4.5) are implemented, but production readiness still depends on long-run deterministic behavior under repeated shell lifecycle operations.
- Current runtime automation already covers representative and medium stress scenarios, but release-critical shell hardening evidence is not centralized in a machine-readable matrix.
- ROADMAP Deliverable 4.6 requires stress/soak lane confidence and a release-readiness checklist that can be validated automatically.

## Goals / Non-Goals

**Goals:**
- Add deterministic long-run shell soak integration coverage that exercises attach/detach cycles, multi-window lifecycle, host capability routing, and policy behavior together.
- Introduce a machine-readable shell production matrix with explicit platform coverage and test-evidence mapping.
- Extend runtime critical-path governance to require shell soak scenarios and fail fast on missing evidence.
- Keep validation aligned with contract-first architecture and runnable in existing CT/IT lanes.

**Non-Goals:**
- Building new runtime shell feature APIs.
- Adding legacy compatibility fallback behavior.
- Introducing performance-benchmark tooling or non-deterministic timing-based assertions.

## Decisions

### 1) Composite shell soak test in automation IT lane
- **Decision:** Add a new automation integration test that repeatedly creates/disposes shell experience scopes and validates managed-window close-out plus policy/capability determinism.
- **Rationale:** validates cross-domain interactions (policy + window lifecycle + host capability) under sustained cycles in a single deterministic workload.
- **Alternative considered:** increase existing isolated stress loop counts only. Rejected because it misses cross-domain integration regressions.

### 2) Machine-readable shell production matrix in `tests/`
- **Decision:** Introduce `tests/shell-production-matrix.json` with platform coverage and executable evidence references.
- **Rationale:** enables automated governance checks and auditable release-readiness evidence without relying on manual doc review.
- **Alternative considered:** maintain matrix only in markdown docs. Rejected due to weak enforceability.

### 3) Governance extension via existing unit governance suite
- **Decision:** Extend `AutomationLaneGovernanceTests` and runtime critical path manifest to validate shell soak entries and matrix evidence references.
- **Rationale:** reuses existing fail-fast governance path and preserves architecture boundaries.
- **Alternative considered:** add separate CLI verifier tool. Rejected as unnecessary complexity for current scope.

## Risks / Trade-offs

- **[Longer automation run time] →** Keep soak cycles bounded and deterministic; focus on lifecycle invariants instead of very large iteration counts.
- **[Evidence drift between matrix and tests] →** Add strict governance checks that verify file and test-method existence.
- **[False confidence from mock-only checks] →** Keep Windows WebView2 teardown stress in required evidence set and include runtime automation lane mapping.

## Migration Plan

1. Add new shell production matrix artifact and governance validation.
2. Add shell soak integration test and map it into runtime critical-path manifest.
3. Run unit governance + automation integration suites.
4. Update OpenSpec tasks/evidence and archive change when all checks pass.

Rollback: remove matrix/governance additions and new soak test files; existing automation lanes continue unchanged.

## Open Questions

- Should M4.6 require platform-specific macOS/Linux shell soak binaries in CI now, or track as a follow-up once dedicated host runners are provisioned?
