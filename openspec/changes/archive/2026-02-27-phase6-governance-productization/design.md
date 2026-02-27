## Context

Phase 5 closeout is complete, but governance implementation still uses Phase 5-specific literals in build evidence payloads and governance test assertions. This creates phase-coupled maintenance debt and conflicts with the contract-driven architecture goal in `PROJECT.md` (G4) and secure deterministic governance posture (G3).  
From `ROADMAP.md`, Phase 5 is explicitly marked completed; the next stage requires governance contracts that can evolve without hardcoding phase numbers.

This change is cross-cutting across OpenSpec artifacts, build orchestration, and governance tests:
- `build/Build.Governance.cs` closeout snapshot target + payload contract
- `build/Build.cs` CI dependency semantics
- `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs` invariant checks
- `openspec/ROADMAP.md` transition clarity

## Goals / Non-Goals

**Goals:**
- Remove phase-number coupling from closeout evidence contracts while preserving deterministic CI semantics.
- Replace Phase 5 literal governance checks with stable semantic invariants.
- Make roadmap transition governance machine-checkable: completed previous phase + declared active next phase.
- Keep release gating aligned with CI evidence contract v2 and existing semantic assertion principles.

**Non-Goals:**
- No runtime feature additions (bridge, adapter, shell, policy behavior unchanged).
- No compatibility fallback for old phase-specific evidence payloads.
- No redesign of target graph outside governance/evidence transition scope.

## Decisions

### Decision 1: Use phase-neutral closeout contract fields
- **Option A (selected):** switch snapshot naming/fields to phase-neutral terms (for example `closeout-snapshot` + explicit phase scope metadata).
- **Option B:** keep Phase 5 identifiers and only patch test literals.
- **Rationale:** Option A removes recurring migration debt and matches long-term contract evolution.

### Decision 2: Govern transitions with semantic invariants, not title strings
- **Option A (selected):** add invariant IDs for transition semantics (previous-phase completed, active-phase declared, closeout evidence linked).
- **Option B:** continue regex/string checks on roadmap headings.
- **Rationale:** semantic invariants are less brittle and consistent with `governance-semantic-assertions`.

### Decision 3: Direct cutover without legacy compatibility branch
- **Option A (selected):** update build/tests/specs to new semantics in one change.
- **Option B:** dual-path governance supporting both old and new phase contracts.
- **Rationale:** repository policy prefers single-path correctness; dual-path adds unnecessary drift risk.

### Testing strategy
- CT: update governance unit tests for new invariant IDs, closeout payload semantics, and CI target dependency assertions.
- Build governance: execute `openspec validate --all --strict` and `nuke Test` to verify deterministic contract checks.
- Release path confidence: run `nuke CiPublish` governance path validation to ensure new evidence contract gates remain intact.

## Risks / Trade-offs

- **[Risk]** Existing automation scripts may still expect old snapshot naming.  
  **Mitigation:** update governed references in the same change and fail fast with explicit diagnostics.

- **[Risk]** Transition invariants can become over-constrained and block unrelated updates.  
  **Mitigation:** scope invariants to contract-critical fields only; avoid layout/text coupling.

- **[Risk]** Roadmap and tests can drift again in future phases.  
  **Mitigation:** centralize transition invariants with stable IDs and require CI enforcement.

## Migration Plan

1. Update spec deltas for phase-neutral closeout governance.
2. Implement build target/payload and governance-test cutover in one branch.
3. Update roadmap transition section and evidence references.
4. Run strict validation and CI-targeted checks; fail on any stale Phase 5 contract path.

## Open Questions

- Should closeout metadata model track only `completedPhase` and `activePhase`, or also include optional `phaseSeries` for long-horizon analytics?
- Should invariant catalog IDs be centralized into one JSON registry file in this change, or kept in code-level constants first and centralized in a follow-up?
