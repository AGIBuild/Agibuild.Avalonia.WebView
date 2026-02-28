## Context

The repository currently keeps deterministic transition governance through machine-checkable phase markers in `openspec/ROADMAP.md` and mirrored constants in `build/Build.Governance.cs`.  
Archived changes indicate Phase 7 deliverables were completed, but transition metadata is still pinned to the prior baseline (`phase6 -> phase7`).  
This mismatch weakens release evidence auditability and can produce false governance failures or misleading closeout payloads.

Relevant alignment:
- Goal: `G4` (contract-driven testability and machine-auditable governance).
- ROADMAP linkage: Phase 7 release-orchestration closeout continuity and next-phase bootstrap.
- Architecture consistency: keep single ownership in governance layer (`Build.Governance.cs`) and invariant assertions in governance tests.

## Goals / Non-Goals

**Goals:**
- Move roadmap and governance transition metadata to one synchronized adjacent baseline.
- Keep closeout archive mapping aligned with the completed phase baseline.
- Preserve deterministic CI diagnostics and lane parity behavior.
- Validate the change via governance-focused CT plus OpenSpec strict validation.

**Non-Goals:**
- No runtime behavior changes in WebView/Bridge/Shell components.
- No schema version bump for closeout snapshot artifacts.
- No package publish or release-tag side effects.

## Decisions

### Decision 1: Use a single-source transition pair and update all dependent assertions in one patch
- **Chosen**: Update `ROADMAP.md`, `Build.Governance.cs`, and governance tests together.
- **Why**: Prevents transient partial states and keeps phase transition atomic.
- **Alternatives considered**:
  1. Update roadmap first, then code/tests in later commit — rejected (temporary deterministic failure window).
  2. Update only governance constants and leave roadmap later — rejected (breaks machine-checkable source of truth).

### Decision 2: Set next baseline to `completed=phase7-release-orchestration`, `active=phase8-bridge-v2-parity`
- **Chosen**: Adjacent rollover based on archived Phase 7 closeout completion and existing Phase 8 workstream naming.
- **Why**: Follows adjacent-pair invariant and avoids non-adjacent jump risk.
- **Alternatives considered**:
  1. Keep `phase6 -> phase7` — rejected (stale baseline).
  2. Jump directly to `phase8 -> phase9` — rejected (no ratified Phase 9 baseline in roadmap artifacts).

### Decision 3: Add a dedicated spec for baseline reconciliation invariants
- **Chosen**: Introduce `phase-baseline-reconciliation` to explicitly govern roadmap/governance atomic updates.
- **Why**: Makes this class of drift first-class and testable.
- **Alternatives considered**:
  1. Only patch existing specs — acceptable but less explicit for future maintainers.

## Risks / Trade-offs

- **[Risk] Incorrect phase id naming causes false gate failures** → Mitigation: assert markers through existing governance tests and strict OpenSpec validation.
- **[Risk] Closeout archive mapping misses one completed change** → Mitigation: include deterministic test assertions for all required archived IDs.
- **[Trade-off] Adds one more governance spec** → Mitigation: keep scope minimal and tied to drift-prevention semantics only.

## Migration Plan

1. Create/finish change artifacts (proposal/design/specs/tasks).
2. Update roadmap transition markers and Phase 7/8 narrative.
3. Update closeout snapshot constants and completed-phase archive list.
4. Update governance tests to assert new transition pair and archive ids.
5. Run verification: targeted governance CT, `openspec validate --all --strict`, then broader CI-equivalent checks.
6. If verification fails, fix and rerun until deterministic pass.

## Open Questions

- Should Phase 8 remain marked active with completed M8.1-M8.5 listed, or should roadmap immediately define Phase 9 as active after this reconciliation?
