## Context

Phase 5 delivered policy-first shell governance and deterministic diagnostics, then follow-up hardening added aggregate metadata budget and profile revision fields. The next gap is contract normalization: budget should be configurable within safe bounds, and profile revision diagnostics should have canonical semantics so CI/agents can reason about outputs without heuristic parsing.

This change spans bridge boundary validation, shell diagnostics normalization, template markers, and governance evidence. It aligns with G3/G4 and extends Phase 5 deliverables 5.3/5.4/5.5 as closeout hardening.

## Goals / Non-Goals

**Goals:**
- Make inbound metadata aggregate budget configurable with deterministic min/max bounded validation.
- Define canonical normalization for profile revision diagnostics (`profileVersion`, `profileHash`) with stable format semantics.
- Keep template guidance aligned with contract v2 (ShowAbout opt-in and revision-aware diagnostics path).
- Preserve deterministic outcomes and machine-checkable testability across CT/IT/governance lanes.

**Non-Goals:**
- No capability surface expansion.
- No changes to policy execution order or deny-by-default system action model.
- No compatibility fallback path for legacy diagnostic contracts.
- No platform/framework expansion.

## Decisions

### Decision 1: Introduce bounded bridge options for metadata budget
- Choice: Add bridge options with configurable aggregate budget constrained by hard bounds (`min`, `max`) and default value.
- Why: Hosts need tunable budget for different event density while runtime must prevent unbounded payload growth.
- Alternative: keep fixed constant only (rejected; lacks host-level governance flexibility).

### Decision 2: Enforce canonical profile revision normalization
- Choice: normalize `profileVersion` by trimming and empty-to-null; normalize `profileHash` to canonical `sha256:<64-lower-hex>` or null if invalid.
- Why: Observability consumers need deterministic field semantics and format stability.
- Alternative: accept arbitrary free-form hash (rejected; not machine-reliable for governance).

### Decision 3: Keep invalid hash non-fatal for policy path
- Choice: invalid revision metadata does not block permission/pruning decisions; diagnostics emit normalized null fields.
- Why: Revision metadata is observability contract, not authorization input; blocking main path would couple audit hints with runtime control.
- Alternative: fail policy path on invalid hash (rejected; creates avoidable runtime fragility).

### Decision 4: Template and governance markers explicitly capture v2 semantics
- Choice: template keeps deny-by-default ShowAbout and adds markerized opt-in snippet + revision-aware diagnostic references.
- Why: maintains G3 guarantees while improving E1/E2 onboarding clarity and governance test resilience.
- Alternative: silently update runtime only (rejected; marker/governance drift risk).

## Risks / Trade-offs

- [Risk] Too-small configured budget could unintentionally deny valid events.  
  Mitigation: bounded range + explicit tests for default, exact, and over-budget branches.
- [Risk] Canonical hash normalization may hide malformed producer data.  
  Mitigation: preserve deterministic null output and cover this branch in tests/governance evidence.
- [Risk] Additional options increase configuration surface complexity.  
  Mitigation: keep single-purpose option object with strict validation and safe defaults.

## Migration Plan

1. Add delta specs for four modified capabilities.
2. Implement bridge options and bounded metadata budget validation.
3. Implement profile revision normalization in shell diagnostic emission.
4. Update template markers and governance assertions.
5. Expand CT/IT evidence matrix and run focused test lanes.
6. Validate change in strict mode, sync main specs, and archive.

Rollback:
- Revert bridge options and normalization to previous fixed/default behavior while preserving existing deterministic envelope checks.

## Open Questions

- Should a future phase expose budget config from template configuration surface or keep it host-code only?
- Should canonical hash support additional algorithms beyond `sha256` in a versioned contract extension?
