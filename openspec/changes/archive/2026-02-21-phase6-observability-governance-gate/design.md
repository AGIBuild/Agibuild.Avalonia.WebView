## Context

Repository strict OpenSpec compliance was recently restored to green, but pipeline enforcement still relies on convention instead of a dedicated CI gate. In parallel, diagnostic schema versioning now exists in runtime contracts and shared assertions, yet evolution governance can drift if build target wiring and assertion usage are not continuously enforced.

This design aligns with G4 (Contract-Driven Testability) and Phase 5 deliverables M5.3/M5.5 by making contract validation and observability schema continuity explicit CI invariants.

## Goals / Non-Goals

**Goals:**
- Add an explicit build target for `openspec validate --all --strict`.
- Make `Ci` and `CiPublish` depend on that target.
- Lock gate continuity through governance tests.
- Extend observability schema-governance requirements to include CI gate continuity and shared expectation source usage.

**Non-Goals:**
- Runtime behavior changes for capability or session diagnostics.
- Additional fallback paths for non-strict spec formats.
- Restructuring unrelated packaging/test targets.

## Decisions

1. **Decision: Introduce a dedicated Nuke target (`OpenSpecStrictGovernance`)**
   - Why: keeps strict validation intent explicit and auditable in build graph.
   - Alternative: inline command in `Ci`/`CiPublish`; rejected for weak reuse and reduced traceability.

2. **Decision: Use checked process execution for strict validation**
   - Why: deterministic fail-fast behavior and uniform diagnostics in Nuke logs.
   - Alternative: best-effort non-checked execution; rejected because governance gates must be hard failures.

3. **Decision: Enforce gate wiring via governance source tests**
   - Why: prevents accidental target-graph drift during future refactors.
   - Alternative: rely only on workflow YAML review; rejected due to delayed detection.

4. **Decision: Keep schema evolution contract tied to shared assertion helper**
   - Why: one source of truth for version expectations across CT/IT/governance lanes.
   - Alternative: per-lane local constants; rejected for drift risk.

## Risks / Trade-offs

- **[Risk] Build runtime increases slightly due to extra validation step** → **Mitigation:** strict validation is fast and runs once per CI target graph.
- **[Risk] Governance tests become brittle to benign naming changes** → **Mitigation:** assert core contract strings/target dependencies only.
- **[Trade-off] Hard gate can block urgent fixes if specs lag** → **Mitigation:** enforce spec-first workflow and keep changes synchronized.

## Migration Plan

1. Add `OpenSpecStrictGovernance` target to Nuke build.
2. Wire target into `Ci` and `CiPublish` dependencies.
3. Add/update governance tests for target presence, strict command, and dependency graph.
4. Run targeted governance tests + strict OpenSpec validation + CI target dry-run.
5. Archive change after successful verification.

## Open Questions

- Should we also expose a standalone workflow/job in GitHub Actions that runs only strict OpenSpec governance for faster PR feedback?
