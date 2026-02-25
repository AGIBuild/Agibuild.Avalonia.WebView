## Context

`system-integration-contract-v2` delivered schema-first inbound validation, `platform.*` namespace gating, and ShowAbout typed action semantics. The archived checklist still left three freeze items: explicit reserved key registry, canonical timestamp wire semantics, and template runtime toggle strategy for ShowAbout. This change is a Phase 5 hardening increment aligned with deliverables 5.3/5.4/5.5.

## Goals / Non-Goals

**Goals:**
- Enforce deterministic reserved-key governance inside `platform.*` metadata for inbound events.
- Normalize inbound event timestamp to canonical UTC millisecond precision before dispatch.
- Expose explicit runtime ShowAbout opt-in marker in app-shell template while preserving default deny.
- Strengthen CT/IT/governance assertions to make these rules machine-checkable.

**Non-Goals:**
- No new capability domain beyond existing v2 system integration flow.
- No backward-compatible dual path for legacy metadata contracts.
- No expansion toward Electron full API parity.

## Decisions

### Decision 1: Reserved-key registry with bounded extension lane
- Choice: Add deterministic key registry for known keys (`platform.source`, `platform.visibility`, etc.) and permit custom keys only under `platform.extension.*`.
- Why: Prevent silent key drift while preserving controlled extensibility.
- Alternative considered: Prefix-only (`platform.*`) acceptance. Rejected because it cannot prevent accidental semantic key forks.

### Decision 2: Canonical timestamp normalization at bridge boundary
- Choice: Normalize `OccurredAtUtc` to UTC and truncate to millisecond precision before event dispatch.
- Why: Stabilizes wire format and test assertions across hosts/runtimes.
- Alternative considered: Validate-only (no normalization). Rejected because producers may emit sub-millisecond variance that hurts deterministic comparisons.

### Decision 3: Template ShowAbout runtime toggle marker
- Choice: Replace hard-coded local const with explicit runtime opt-in helper (`IsShowAboutEnabledFromEnvironment`) defaulting to disabled.
- Why: Keeps default deny (G3) but makes opt-in strategy visible and scriptable.
- Alternative considered: Always enable ShowAbout in template. Rejected due to secure-by-default principle.

## Risks / Trade-offs

- [Risk] Registry too strict could reject legitimate platform evolution keys.  
  → Mitigation: keep extension lane `platform.extension.*` and document reserved keys.
- [Risk] Timestamp truncation may hide sub-millisecond diagnostics fidelity.  
  → Mitigation: apply only to contract payload; diagnostics still carry correlation/outcome.
- [Risk] Environment-driven toggle might be misread as production recommendation.  
  → Mitigation: keep explicit marker comments and governance assertions for default deny.

## Migration Plan

1. Add registry + normalization in bridge boundary validation path.
2. Update template app-shell toggle strategy and event metadata producers.
3. Add/adjust CT + IT + governance tests and matrices.
4. Run `nuke Test` and `nuke Coverage` as release-grade validation.
5. Archive change after strict validation pass.

Rollback: revert this change as a single contract hardening increment; no dual-mode runtime path retained.

## Open Questions

- Should reserved keys be surfaced as public constants for host-side compile-time reuse?
- Do we need stricter `platform.extension.*` value character constraints in this phase?

## Testing Strategy

- **CT (unit):** registry allow/deny branches, extension lane behavior, timestamp normalization determinism, ShowAbout toggle default semantics.
- **IT (automation):** roundtrip with reserved keys + extension keys, canonical timestamp assertion across dispatch flow.
- **Governance:** template marker checks for runtime toggle path and no baseline bypass; bridge source marker assertions for registry and normalization constants.
