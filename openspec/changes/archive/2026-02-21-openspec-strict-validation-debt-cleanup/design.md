## Context

The repository accumulated legacy spec files that predated current strict OpenSpec schema checks. This caused repeated full-repository validation failures and reduced the reliability of governance evidence expected by Phase 5 (M5.5 Production Governance). The runtime architecture remains contract-first (`WebViewCore` as semantic owner, adapter abstractions platform-isolated), so specification quality must be deterministic and machine-checkable to stay aligned with G4.

## Goals / Non-Goals

**Goals:**
- Re-establish a deterministic strict-validation baseline for all repository specs.
- Normalize spec structure and requirement wording without changing runtime behavior.
- Keep remediation bounded to spec governance so CI and release evidence can rely on strict checks.

**Non-Goals:**
- Introduce new runtime capabilities or API behavior changes.
- Add compatibility parsing for deprecated spec styles.
- Rework test architecture beyond what strict governance evidence already requires.

## Decisions

1. **Decision: Batch remediation directly in existing main specs**
   - Rationale: Validation failures were distributed across many existing spec files; direct normalization minimizes divergence windows.
   - Alternatives considered:
     - Per-capability incremental cleanup changes: safer isolation, but prolonged red baseline and higher coordination cost.
     - Validator compatibility fallback for old format: rejected because it weakens governance and conflicts with strict policy intent.

2. **Decision: Enforce normative requirement wording (`SHALL`/`MUST`)**
   - Rationale: Strict tooling relies on unambiguous normative markers for machine-checkable contract semantics.
   - Alternatives considered:
     - Permit mixed prose style: rejected due to inconsistent parseability and audit ambiguity.

3. **Decision: Require scenario completeness for every requirement**
   - Rationale: Scenario-backed requirements map to executable CT/IT expectations and preserve contract-testability principles.
   - Alternatives considered:
     - Allow requirement-only entries: rejected because unverifiable statements weaken traceability.

4. **Decision: Verify with full strict validation gate**
   - Rationale: `openspec validate --all --strict` is the only authoritative repository-wide success criterion for this cleanup.
   - Alternatives considered:
     - Spot validation on touched specs only: rejected because hidden failures elsewhere would remain.

## Risks / Trade-offs

- **[Risk] Broad spec edits can accidentally alter intent** → **Mitigation:** constrain edits to structure/normative phrasing and keep requirement semantics intact.
- **[Risk] Future contributors reintroduce lax formatting** → **Mitigation:** keep strict full-repo validation as mandatory governance gate.
- **[Trade-off] Large one-shot cleanup reduces review granularity** → **Mitigation:** preserve explicit change artifacts and deterministic verification evidence.

## Migration Plan

1. Normalize strict-format violations in affected repository specs.
2. Repair normative wording and missing scenario blocks.
3. Execute `openspec validate --all --strict` until zero failures.
4. Archive the cleanup change as governance evidence.

Rollback is straightforward: revert spec-only commits if needed; no runtime deployment rollback path is required.

## Open Questions

- Should strict validation be promoted to a dedicated CI required check for all PRs (not only governance-oriented changes)?
