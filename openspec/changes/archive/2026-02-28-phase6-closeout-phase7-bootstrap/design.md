## Context

Phase transition governance has already been productized in Phase 6 (`M6.2`, `M6.3`), but repository baselines still reflect the previous machine-checkable pair (`phase5` completed, `phase6` active). The transition gate is already lane-aware and deterministic, so Phase 6 closeout should be implemented as a baseline rollover, not as new governance mechanics.

Current constraints:
- `ReleaseCloseoutSnapshot` hardcodes completed/active phase constants and validates against `ROADMAP.md`.
- Governance tests assert roadmap markers and expected closeout archive IDs.
- Several archived specs still keep `TBD` purpose placeholders, which weakens strict-governance quality signals.

## Goals / Non-Goals

**Goals:**
- Move machine-checkable roadmap markers to `phase6-governance-productization` (completed) and `phase7-...` (active).
- Keep transition gate invariants deterministic across `Ci` and `CiPublish` after rollover.
- Refresh closeout evidence mapping and assertions to Phase 6 closeout artifacts.
- Remove all archived `TBD` purpose placeholders to keep strict validation baseline release-ready.

**Non-Goals:**
- No new CI lane topology, no new invariant IDs, and no behavior change in runtime/shell capabilities.
- No Phase 7 feature implementation; only Phase 7 activation framing and governance baseline bootstrap.

## Decisions

### Decision 1: Use synchronized baseline rollover in roadmap + build constants + governance tests
- **Chosen**: Update all three layers in one change (`ROADMAP.md`, `Build.Governance.cs`, `AutomationLaneGovernanceTests.cs`) and validate with full test/coverage + strict OpenSpec checks.
- **Alternative A**: Update roadmap only, defer constants/tests.
  - Rejected: guaranteed deterministic failures because build/tests enforce marker consistency.
- **Alternative B**: Add compatibility fallback for both old/new phase pairs.
  - Rejected: violates deterministic single-source governance and would add unnecessary branching complexity.

### Decision 2: Keep transition gate invariants generic; only rotate evidence baselines
- **Chosen**: Reuse existing invariants (`GOV-022`, `GOV-024`, `GOV-025`, `GOV-026`) and rotate expected phase/evidence payload values.
- **Alternative A**: Introduce new invariant IDs for each phase transition.
  - Rejected: creates invariant churn and weakens long-term diagnostic stability.

### Decision 3: Resolve `TBD` purpose placeholders directly in canonical spec files
- **Chosen**: Replace placeholder text with concise purpose statements in each affected spec.
- **Alternative A**: Add validator exceptions for known archived placeholders.
  - Rejected: weakens strict governance and masks quality debt instead of removing root cause.

## Risks / Trade-offs

- **[Risk]** Wrong Phase 6 closeout archive IDs in roadmap/evidence mapping -> **Mitigation**: derive IDs from existing archive entries and assert via governance test.
- **[Risk]** Roadmap status edits accidentally break regex-based governance assertions -> **Mitigation**: preserve machine-checkable marker format and run full unit governance suite.
- **[Trade-off]** Tight coupling between roadmap text and governance tests remains -> **Mitigation**: keep coupling intentional and explicit for deterministic CI fail-fast behavior.

## Migration Plan

1. Author delta/new specs for phase rollover and strict purpose-finalization requirements.
2. Update roadmap phase status, transition markers, and Phase 6 closeout evidence references.
3. Update build closeout constants and closeout archive ID baseline.
4. Update governance tests to enforce new phase markers and archive IDs.
5. Replace all `TBD` purpose placeholders in affected canonical specs.
6. Run `dotnet test`, `nuke Test`, `nuke Coverage`, `openspec validate --all --strict`.

Rollback strategy: revert this single change set to restore previous baseline; no data migration required.

## Open Questions

- Final Phase 7 display title and milestone naming in roadmap should stay stable once Phase 7 implementation change is created.
