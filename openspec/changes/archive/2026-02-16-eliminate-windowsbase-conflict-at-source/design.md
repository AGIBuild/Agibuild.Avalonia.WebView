## Context

`MSB3277` (`WindowsBase` conflict) is currently classified as known baseline in warning governance. This keeps CI pass rates stable but weakens signal quality in Phase 3 release hardening.

The root source is WebView2 package target injection (`build/Common.targets`) adding WPF/WinForms references for modern .NET managed projects, which introduces `WindowsBase` conflict diagnostics during governed builds. This work aligns with:
- Goal: **G4 (Contract-Driven Testability)** by keeping CI diagnostics deterministic and actionable.
- ROADMAP Phase 3 (Polish & GA), quality/readiness ratchet under deliverable **3.8** (release readiness and audit posture).

Architecture constraint from `docs/agibuild_webview_design_doc.md`: runtime semantics remain centralized in `WebViewCore`; build/dependency policy changes must not duplicate runtime responsibilities.

## Goals / Non-Goals

**Goals:**
- Eliminate `WindowsBase` conflict warnings from governed build targets instead of carrying them in baseline metadata.
- Make Windows dependency boundaries explicit so non-Windows hosts avoid Windows-only conflict paths.
- Preserve Windows runtime package correctness for consumers.
- Preserve requirement that any host can build and pack all platform package artifacts.
- Upgrade warning governance policy so reappearance of this warning class fails as regression.

**Non-Goals:**
- No new runtime/product features in `WebViewCore` or adapters.
- No broad zero-warning campaign for unrelated warning categories.
- No replacement of current warning-governance reporting mechanism.

## Decisions

1. **Disable WebView2 target injection in affected projects**
   - Chosen: set package asset filters so `build`/`buildTransitive` from `Microsoft.Web.WebView2` are not imported in the Windows adapter and pack projects.
   - Why: prevents automatic WPF/WinForms reference injection that causes `WindowsBase` conflict noise.
   - Alternative: keep auto-import and baseline whitelist; rejected because warning source remains active.

2. **Use explicit compile-time binding to WebView2.Core in Windows adapter**
   - Chosen: keep package reference for restore graph but bind compilation through explicit `Microsoft.Web.WebView2.Core.dll` reference via package path property.
   - Why: removes warning at source and improves long-term CI signal quality.
   - Alternative: remove WebView2 dependency declaration; rejected due to consumer runtime breakage risk.

3. **Enforce hard regression rule for this warning class**
   - Chosen: treat any `MSB3277` + `WindowsBase` occurrence as `new-regression`.
   - Why: guarantees the conflict class cannot silently return.
   - Alternative: keep category `known-baseline`; rejected after source elimination because it masks regressions.

4. **Preserve package/runtime contract while isolating build-host effects**
   - Chosen: keep package dependency metadata to `Microsoft.Web.WebView2` for consumers while isolating compile references in project build graphs.
   - Why: avoids breaking downstream apps while fixing CI host behavior.
   - Alternative: remove WebView2 dependency declaration from package graph; rejected due to risk of consumer runtime failures.

5. **Testing anchored in build governance and existing test architecture**
   - Chosen: validate with governance CT plus target builds on Windows and non-Windows lanes; keep runtime CT/IT unchanged unless dependency edits require updates.
   - Why: this is a build-graph correctness change, not runtime semantics change.

## Testing Strategy (CT/IT/MockBridge)

- **CT (primary)**:
  - Update/add warning governance tests to enforce that `WindowsBase` conflict is not accepted baseline.
  - Add classifier tests for recurrence path (`new-regression` + gate failure).
- **Build verification (cross-host)**:
  - Run governed build/pack targets on Windows and non-Windows CI contexts and verify no accepted `WindowsBase` conflicts.
- **IT/MockBridge**:
  - No new runtime behavior expected; existing IT/MockBridge suites are regression safety checks only.

## Risks / Trade-offs

- **[Risk] Hidden transitive edge still triggers conflict in a subset of targets** → Mitigation: validate all governed build targets (adapter, pack, integration/unit test projects), not just one project.
- **[Risk] Over-tight gating increases short-term CI failures** → Mitigation: land dependency and governance changes in one change-set with deterministic synthetic checks.
- **[Trade-off] Slightly higher maintenance in project reference layout** → Accepted for stronger quality signal and simpler CI triage.

## Migration Plan

1. Introduce dependency boundary changes in affected `.csproj` files.
2. Update warning governance classification policy and baseline schema/data for this class.
3. Update governance tests (including synthetic checks) to encode new invariant.
4. Run cross-host restore/build/test verification.
5. Merge once governed warnings are stable and report artifacts confirm zero accepted `WindowsBase` conflicts.

Rollback:
- Revert dependency-boundary edits and governance rule changes together as one unit to avoid partial-policy inconsistency.

## Open Questions

- Keep `windowsBaseConflicts` as an empty schema field for compatibility, or remove the field in baseline v2?
