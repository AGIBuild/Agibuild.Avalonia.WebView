## Context

This change targets Phase 3 (Polish & GA) release readiness items from the roadmap:
- **Deliverable 3.5**: GTK/Linux smoke validation
- **Deliverable 3.8**: API surface review + breaking change audit

Current gaps:
- The GTK adapter (`Agibuild.Avalonia.WebView.Adapters.Gtk`) is explicitly not production-ready and contains TODOs for wiring native DevTools integration.
- Repository test projects have **xUnit v3 version drift** (repo tests, template tests, and sample tests use different `xunit.v3` versions), increasing CI and maintenance risk.
- The **API surface review** capability exists as a spec, but the change needs a repeatable way to produce and store the review outputs with traceability to executable evidence (G4).
- Several OpenSpec specs still contain **TBD Purpose** placeholders (documentation-only hygiene).

Constraints:
- Stay consistent with the contract-driven architecture: `WebViewCore` remains the single semantic authority; adapters implement platform mechanics only.
- Testing must be deterministic where possible and must integrate with existing lanes (`ContractAutomation` vs `RuntimeAutomation`).

## Goals / Non-Goals

**Goals:**
- Make Linux/GTK readiness measurable and repeatable via a smoke validation lane aligned with Phase 3 Deliverable 3.5.
- Wire GTK DevTools open/close to the native WebKitGTK inspector APIs where supported (aligning with E2 “Dev Tooling” expectations).
- Align xUnit v3 package versions across repository-owned test projects, templates, and samples to eliminate drift.
- Produce an API surface review output (inventory + breaking change classification + prioritized action plan) and store it in a consistent, reviewable location (Deliverable 3.8).
- Clean up TBD Purpose placeholders in existing specs (documentation-only; no behavior change).

**Non-Goals:**
- No new end-user WebView features (navigation, cookies, bridge, SPA hosting) beyond readiness/validation work.
- No performance benchmarking work in this change.
- No adapter parity work outside GTK/Linux readiness.
- No broad documentation initiative beyond minimal spec-purpose hygiene.

## Decisions

### Decision: Define GTK readiness as a lane with explicit acceptance criteria

**Choice:** Introduce a GTK/Linux readiness capability spec and implement it as a **Linux runtime smoke validation lane** with explicit pass/fail criteria and published artifacts.

**Why:** Phase 3 Deliverable 3.5 is fundamentally about reducing uncertainty (“Linux is untested”) by turning it into repeatable evidence. A lane gives stable CI semantics and aligns with the existing `RuntimeAutomation` reporting model.

**Alternatives considered:**
- **Manual checklist only**: low effort but non-repeatable; fails G4-style evidence expectations.
- **Full integration test suite on Linux**: higher confidence but too costly for “smoke validation” scope; risks delaying GA readiness.

### Decision: Wire GTK DevTools via WebKitGTK inspector APIs (show/close)

**Choice:** Implement `OpenDevTools()` / `CloseDevTools()` for GTK using the native WebKitGTK inspector APIs (`webkit_web_inspector_show` / `webkit_web_inspector_close`) when `EnableDevTools` is enabled.

**Why:** GTK uniquely has a supported native inspector API; keeping it as TODO undermines E2 and weakens the cross-platform DevTools toggle abstraction.

**Alternatives considered:**
- **Leave as no-op**: inconsistent UX; violates the intent of DevTools toggle on a platform that supports it.
- **Add private/unsupported hacks**: rejected; this project favors explicit, supported platform behavior.

### Decision: Align xUnit versions by selecting a single repository baseline

**Choice:** Establish a single `xunit.v3` baseline version for all repository-owned tests (including templates and samples) and make it discoverable/maintainable (preferably via shared props/centralization).

**Why:** Version drift causes subtle differences in discovery/execution/analyzers and increases maintenance cost. Alignment supports Phase 3 GA readiness quality.

**Alternatives considered:**
- **Keep drift and rely on CI**: defers problems and increases false negatives/positives.
- **Pin each project independently**: works short-term but repeats drift over time without governance.

### Decision: Produce API surface review outputs in a deterministic, toolable format

**Choice:** Generate and store API review outputs in a stable, diff-friendly format (sorted inventory + categorized action items) and keep an explicit mapping to executable evidence where applicable.

**Why:** Deliverable 3.8 is a “review”, but it must be actionable. Deterministic output enables PR review and future regression tracking.

**Alternatives considered:**
- **Purely manual notes**: low friction but loses auditability and is hard to repeat.
- **Hard-gating public API with analyzers immediately**: useful long-term, but may be too disruptive for a first GA readiness review. This can be a follow-up once the initial audit is complete.

## Risks / Trade-offs

- **[Risk] Linux CI environment may not support running WebKitGTK UI without a display server** → **Mitigation**: run smoke validation under Xvfb; keep the lane explicitly scoped and report “skipped with reason” if prerequisites are absent (consistent with runtime automation validation principles).
- **[Risk] DevTools inspector APIs differ across WebKitGTK versions** → **Mitigation**: keep the behavior minimal (show/close) and validate on the CI image; document supported matrix if needed.
- **[Risk] xUnit version alignment may break runner/analyzer behavior** → **Mitigation**: align runner packages together; run full CI test suite; keep changes mechanical and revertible.
- **[Risk] API inventory generation can be non-deterministic** → **Mitigation**: enforce stable ordering, stable formatting, and explicit inclusion rules; keep output as a committed review artifact for PR diffing.

