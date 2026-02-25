## Context

Three closeout streams have landed (status authority, evidence automation, and contract hardening), but governance currently validates them in isolation. A compact set of invariant checks should lock the final baseline.

## Goals / Non-Goals

**Goals:**
- Guard Phase 5 roadmap completion markers.
- Guard matrix/manifest ID consistency for critical shell scenarios.
- Guard template and diagnostic regression entry-point discoverability.

**Non-Goals:**
- No runtime test behavior changes.
- No additional CI lane.
- No markdown mutation in tests.

## Decisions

### Decision 1: Add dedicated closeout governance fact
- Introduce one focused test for roadmap + evidence marker checks.

### Decision 2: Use deterministic shared ID set for cross-manifest validation
- Maintain one local set of shared shell governance IDs and assert presence in both artifacts.

### Decision 3: Reuse existing source-marker strategy
- Keep string-marker based checks for template regression and build target presence for low maintenance overhead.

## Risks / Trade-offs

- [Risk] Marker-based checks can be brittle if refactors rename symbols.  
  → Mitigation: keep checks bound to contract-level identifiers, not implementation details.

## Testing Strategy

- Run governance tests suite.
- Run full `nuke Test` and `nuke Coverage` in final validation stage.
- Keep OpenSpec strict validation green.
