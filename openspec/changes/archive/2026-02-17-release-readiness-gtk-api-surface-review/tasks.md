## 1. GTK/Linux production readiness (ROADMAP Phase 3 Deliverable 3.5)

- [x] 1.1 Wire GTK DevTools toggle to native WebKitGTK inspector APIs (AC: `OpenDevTools()` shows inspector and `CloseDevTools()` closes it on Linux with WebKitGTK)
- [x] 1.2 Add GTK/Linux runtime smoke coverage for core flows (navigation start/complete, cancel via `NavigationStarted.Cancel`, minimal `InvokeScriptAsync`, minimal WebMessage path) (AC: tests pass locally on Linux without sleeps)
- [x] 1.3 Integrate GTK/Linux smoke lane into CI reporting and artifacts (AC: CI Linux run publishes lane report + test results, and failures are actionable)
- [x] 1.4 Ensure lane explicitly reports “skipped with reason” when prerequisites are missing (AC: report shows skip reason instead of silent omission)

## 2. xUnit v3 version alignment (GA readiness quality)

- [x] 2.1 Inventory xUnit v3 package versions across repo tests, templates, and samples; select a single baseline (AC: baseline is applied consistently across all repo-owned test projects)
- [x] 2.2 Update package references/props so all repo-owned test projects use the baseline (AC: `dotnet test` passes for unit, integration automation, template tests, and sample tests)
- [x] 2.3 Add governance to fail fast on xUnit version drift (AC: introducing a mismatched `xunit.v3` version causes a deterministic governance failure with a clear diagnostic)

## 3. API surface review execution (ROADMAP Phase 3 Deliverable 3.8)

- [x] 3.1 Generate an up-to-date public API inventory for the shipped assemblies (AC: inventory is complete and diff-friendly)
- [x] 3.2 Update `docs/API_SURFACE_REVIEW.md` with current findings, breaking-change classification, and prioritized action items (AC: action items include priority + breaking/non-breaking classification)
- [x] 3.3 Add evidence traceability pointers in the review (AC: boundary-sensitive APIs link to at least one executable test, or an explicit “missing evidence” action item)

## 4. Spec hygiene: remove TBD Purpose placeholders (documentation-only)

- [x] 4.1 Update `openspec/specs/build-pipeline-resilience/spec.md` Purpose (AC: Purpose is 1–2 sentences, no longer TBD)
- [x] 4.2 Update `openspec/specs/runtime-automation-validation/spec.md` Purpose (AC: Purpose is 1–2 sentences, no longer TBD)
- [x] 4.3 Update `openspec/specs/blocking-wait-governance/spec.md` Purpose (AC: Purpose is 1–2 sentences, no longer TBD)
- [x] 4.4 Confirm no remaining “TBD ... Update Purpose after archive” placeholders (AC: repository search returns zero matches)

## 5. End-to-end validation

- [x] 5.1 Run the governed CI target locally and ensure all tests pass (AC: `build.ps1 -Target Ci` completes without regressions on the developer host; CI is green after PR)
