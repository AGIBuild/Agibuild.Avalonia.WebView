## Context

Repository-level branding has already moved to Agibuild.Fulora in roadmap and narrative docs, but technical identity still uses legacy package and namespace names in contracts, template metadata, release conventions, and API documentation references.
This creates a split identity: users see one product name while code and artifacts expose another.  
The change is cross-cutting (core contracts, abstractions, templates, release, docs), has migration complexity, and requires deterministic CI governance updates.

## Goals / Non-Goals

**Goals:**
- Establish `Agibuild.Fulora.*` as the canonical code/package identity across governed surfaces.
- Execute a hard cutover with no legacy compatibility path.
- Keep runtime semantics unchanged (navigation/policy/shell behavior stays identical).
- Ensure migration is testable via CT/governance checks and template validation.

**Non-Goals:**
- No feature expansion in bridge, shell, adapters, or security model.
- No compatibility alias/forwarding/deprecation bridge for legacy identifiers.
- No platform coverage or performance target changes.

## Decisions

### Decision 1: Hard cutover to canonical identity
- **Choice:** Canonical name becomes `Agibuild.Fulora.*`; legacy names are removed from governed and release-facing surfaces in the same release with no alias, forwarding type, or compatibility package.
- **Rationale:** Enforces one source of truth and avoids prolonged identity drift.
- **Alternative rejected:** Compatibility windows/aliases (extend ambiguity and maintenance burden).

### Decision 2: Contract-first migration order
- **Choice:** Update contract specs first (`webview-core-contracts`, `webview-adapter-abstraction`), then template/release/docs specs.
- **Rationale:** Prevents implementation-led drift and keeps governance deterministic.
- **Alternative rejected:** Update docs/templates first and postpone contract changes (would keep API identity ambiguous).

### Decision 3: Governance-enforced naming invariants
- **Choice:** Extend governance checks to fail when any governed artifact uses legacy canonical naming.
- **Rationale:** Avoids silent reintroduction during future refactors.
- **Alternative rejected:** Manual review only (non-deterministic).

### Decision 4: Testing strategy remains CT/IT stable
- **Choice:** Add/adjust unit governance tests and template validations; no new runtime IT lane required for this rename-only change.
- **Rationale:** Change is identity-layer, not behavior-layer.

## Risks / Trade-offs

- **[Risk] Consumer upgrade friction** -> **Mitigation:** explicit hard-cutover release notes and deterministic migration checklist.
- **[Risk] Spec/code mismatch during phased rollout** -> **Mitigation:** Contract-first ordering and strict OpenSpec validation in CI.
- **[Risk] Tooling/script name breakage** -> **Mitigation:** Update release/template/governance artifacts in same change set.

## Migration Plan

1. Land spec deltas for canonical `Agibuild.Fulora` naming and hard-cutover requirements.
2. Implement package/namespace/metadata changes without compatibility shims.
3. Update template and release artifacts.
4. Update docs and API site identity references.
5. Enforce new naming invariants in governance tests.

Rollback: revert the full rename commit set as one unit; no partial compatibility fallback is allowed.

## Open Questions

- None.
