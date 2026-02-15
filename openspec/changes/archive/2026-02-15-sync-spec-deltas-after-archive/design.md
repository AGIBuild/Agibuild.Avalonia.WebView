## Context

This change addresses a process-integrity gap discovered during the previous archive: spec sync failed for `webview-contract-semantics-v1` due to normative-language validation on one requirement body. The repository is in Phase 3 (Polish & GA), where governance quality and archive/sync closure are release-critical (ROADMAP 3.8).  
Architecture guidance from `docs/agibuild_webview_design_doc.md` requires contract-first, machine-verifiable semantics and no hidden behavior drift.

## Goals / Non-Goals

**Goals:**
- Make `webview-contract-semantics-v1` pass normative validation for the targeted requirement.
- Keep the fix scoped to spec wording only (no behavior changes).
- Prove archive+sync closure works end-to-end without `--skip-specs`.

**Non-Goals:**
- No implementation code changes.
- No broad cleanup of unrelated legacy spec-format issues.
- No changes to product scope, roadmap phase boundaries, or runtime semantics.

## Decisions

1. **Use a single-capability delta (modified requirement only).**  
   - Decision: Modify only `webview-contract-semantics-v1` in this change.  
   - Why: Minimal blast radius, fast closure, and explicit traceability.  
   - Alternative considered: batch-fix multiple spec files now.  
   - Rejected because it mixes unrelated migration work with a targeted release blocker.

2. **Strengthen requirement-body wording, not scenarios only.**  
   - Decision: Add explicit SHALL semantics in the requirement body itself.  
   - Why: Validation targets requirement text; bullets/scenarios alone are insufficient in some parser paths.  
   - Alternative considered: leave body unchanged and add extra SHALL in bullets only.  
   - Rejected because it may still fail the same validator path.

3. **Enforce closure with a deterministic command loop.**  
   - Decision: Run `openspec validate <change>` -> `openspec archive <change> -y` (sync enabled) -> post-archive validation/status checks.  
   - Why: Provides auditable evidence that archive/sync no longer requires bypass flags.  
   - Alternative considered: archive with `--skip-specs` and defer sync.  
   - Rejected because it preserves the original governance debt.

## Risks / Trade-offs

- **[Risk] Validator behavior differs across versions** -> Mitigation: keep wording explicitly normative in requirement body and scenarios.
- **[Risk] Hidden sync failures from unrelated spec baselines** -> Mitigation: scope closure evidence to change-level validate + archive/sync for this change.
- **[Trade-off] Narrow scope leaves historical spec debt untouched** -> Accepted for this change; schedule debt cleanup as a separate effort.
