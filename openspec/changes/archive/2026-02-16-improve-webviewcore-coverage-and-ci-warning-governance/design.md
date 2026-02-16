## Context

Coverage reports show `WebViewCore` at 94.3% with uncovered tail branches concentrated in lifecycle/dispatch/error-guard paths. At the same time, CI emits recurring warning noise (`WindowsBase` assembly conflict warnings and xUnit analyzer warnings), which reduces signal quality for release gates.  
This change targets Phase 3 quality hardening (ROADMAP 3.4) and governance readiness (ROADMAP 3.8), while preserving contract-first architecture from `docs/agibuild_webview_design_doc.md` (runtime semantics owned by `WebViewCore`, deterministic testing via mock harness).

## Goals / Non-Goals

**Goals:**
- Raise confidence on `WebViewCore` low-coverage branch hotspots using deterministic CT/automation evidence.
- Make CI warning handling explicit, auditable, and bounded for `WindowsBase` conflicts and xUnit analyzer warnings.
- Prevent warning/coverage regressions from silently re-entering the pipeline.

**Non-Goals:**
- No new product/runtime feature behavior.
- No one-shot elimination of all repository warnings.
- No replacement of current test lane architecture.

## Decisions

1. **Hotspot-first coverage strategy (instead of global blanket test expansion).**  
   - Chosen: maintain a curated hotspot map for `WebViewCore` low-covered branches and require targeted tests for each hotspot.  
   - Why: maximizes risk reduction per test added and avoids low-value test inflation.  
   - Alternative: chase aggregate percentage only; rejected due to poor traceability.

2. **Warning governance with explicit ownership and machine-readable outputs.**  
   - Chosen: classify warnings (`known-baseline`, `actionable`, `new-regression`) and publish a report artifact in CI.  
   - Why: enables deterministic policy decisions and trend tracking.  
   - Alternative: rely on raw console logs; rejected because it is non-actionable at scale.

3. **Bounded suppression policy for analyzer/build warnings.**  
   - Chosen: suppressions require scope, owner, rationale, and expiration/review point.  
   - Why: avoids permanent warning debt while permitting necessary temporary exceptions.  
   - Alternative: blanket `NoWarn`; rejected as governance anti-pattern.

4. **Testing strategy anchored in existing harness lanes.**  
   - Chosen: use ContractAutomation for deterministic branch-path assertions and RuntimeAutomation only where runtime-only behavior is involved.  
   - Why: consistent with G4 and existing lane architecture.

## Testing Strategy (CT/IT/MockBridge)

- **CT (primary):** add/expand deterministic tests around uncovered `WebViewCore` branches (dispatch failure mapping, lifecycle short-circuits, event forwarding guards, completion fallback branches).
- **Governance CT:** add tests that parse warning outputs/manifests and fail on unclassified/new actionable warnings.
- **IT/Automation (selective):** only for hotspots requiring real adapter/runtime behavior.
- **Verification:** run `nuke Coverage` and lane targets; publish coverage + warning governance artifacts.

## Risks / Trade-offs

- **[Risk] Overfitting tests to implementation internals** → Mitigation: keep hotspot assertions framed by contract-level outcomes first, internals only where unavoidable.
- **[Risk] Warning policy too strict causing noisy failures initially** → Mitigation: seed baseline classification with explicit owner/rationale, then ratchet.
- **[Trade-off] Added governance artifacts increase maintenance** → Accepted for long-term signal quality and release safety.

## Migration Plan

1. Introduce specs and manifests for hotspot and warning governance.
2. Implement targeted tests and warning-classification reporting.
3. Establish baseline snapshot in CI.
4. Enforce fail-on-regression for new actionable warnings and missing hotspot evidence.

## Open Questions

- Should `WindowsBase` conflict be treated as temporary baseline or immediate fail once project graph can be normalized?
- Do we require zero xUnit analyzer warnings globally, or zero on touched files first then ratchet repository-wide?
