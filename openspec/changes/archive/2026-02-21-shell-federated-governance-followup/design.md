## Context

The archived `shell-policy-profile-federated-system-integration` change established explicit ShowAbout allowlisting, bounded tray metadata keys/values, and federated menu pruning. Three follow-up decisions remain to fully stabilize governance contracts for long-term CI/agent operations: (1) a cross-platform total metadata payload budget, (2) profile revision attribution in federated pruning diagnostics, and (3) clearer template-level ShowAbout opt-in guidance without changing deny-by-default semantics.

This is a cross-cutting update touching runtime bridge validation, shell diagnostic contracts, template markers, and governance evidence. It aligns with Phase 5 deliverables 5.3, 5.4, and 5.5 and advances G3/G4 guarantees.

## Goals / Non-Goals

**Goals:**
- Introduce deterministic global metadata payload budget validation for inbound system-integration events.
- Extend federated pruning diagnostics with profile revision identity (`profileVersion`, `profileHash`) for audit traceability.
- Add explicit template markerized ShowAbout opt-in snippet while preserving default deny behavior.
- Keep CT/IT/governance evidence machine-checkable and deterministic.

**Non-Goals:**
- No expansion of system action surface or capability gateway breadth.
- No fallback/dual-path compatibility logic for legacy payload handling.
- No changes to existing default deny semantics for ShowAbout.
- No additional platform adapters or bundled-browser API parity scope.

## Decisions

### Decision 1: Add global metadata payload budget in bridge boundary validation
- Choice: Keep current per-entry and key/value bounds, and add a strict aggregate UTF-16 length budget across all metadata key/value pairs.
- Why: Entry-wise constraints alone do not cap worst-case payload footprint; aggregate budget creates deterministic cross-platform upper bound.
- Alternative considered: byte-length budget based on UTF-8 encoding (rejected now due platform-encoding variability in diagnostics and test baselines).

### Decision 2: Keep budget failure as deny-before-policy
- Choice: If aggregate metadata budget exceeds threshold, return deterministic deny before policy/provider execution, same stage as existing metadata envelope validation.
- Why: Maintains single ownership of payload boundary enforcement and avoids policy-layer duplication.
- Alternative considered: convert to policy failure branch (rejected because payload schema validation is a bridge contract invariant, not business policy).

### Decision 3: Add profile revision fields to pruning diagnostics
- Choice: Extend shell profile diagnostic payload with optional `ProfileVersion` and `ProfileHash` fields sourced from `WebViewSessionPermissionProfile`.
- Why: Federated pruning audit needs stable profile identity beyond human-readable name; version/hash allows deterministic trace in CI and release evidence.
- Alternative considered: infer profile revision from `ProfileIdentity` naming conventions (rejected as brittle and non-contractual).

### Decision 4: Template demonstrates opt-in snippet without changing default behavior
- Choice: Add explicit commented snippet/flag marker for ShowAbout allowlist opt-in in app-shell template and governance tests.
- Why: Improves DX discoverability (E1/E2) while preserving policy-first deny-by-default semantics (G3).
- Alternative considered: enable ShowAbout by default in template (rejected due security model regression).

## Risks / Trade-offs

- [Risk] Aggregate length budget may require tuning for real-world metadata payloads.  
  Mitigation: start with conservative cap and cover boundary tests (`exact budget`, `over budget`).
- [Risk] Profile hash field can drift if profile authors use unstable hash generation.  
  Mitigation: keep field optional but contract-stable; governance tests assert propagation when present.
- [Risk] Template marker may be mistaken for default enablement.  
  Mitigation: enforce deny-by-default assertions in governance tests and explicit marker wording.

## Migration Plan

1. Add delta specs for four modified capabilities.
2. Implement runtime bridge aggregate budget validation and diagnostics reason branch.
3. Extend profile diagnostic data contract and federated pruning diagnostic emission path.
4. Update template app-shell marker and web demo/governance assertions.
5. Add/adjust CT/IT/governance matrix coverage and run focused unit/integration lanes.
6. Validate change with `openspec validate <change> --strict`, then sync specs and archive.

Rollback:
- Revert only the aggregate budget branch and profile revision fields while keeping previously shipped allowlist and federated order semantics intact.

## Testing Strategy

- **CT (unit)**: metadata budget exact/overflow branches, diagnostics fields presence, template marker assertions.
- **IT (automation)**: mixed inbound events (within budget + overflow) and federated pruning diagnostics with profile revision fields.
- **MockAdapter**: preserve deterministic contract tests without browser dependency, consistent with G4.

## Open Questions

- Should the aggregate budget become configurable via shell options in a later phase, or remain fixed contract constant?
- Should `ProfileHash` require a specific algorithm label (for example `sha256:<hex>`) at contract level?
