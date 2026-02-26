## Why

Phase 3 (Polish & GA) requires GTK/Linux smoke validation (ROADMAP Phase 3, Deliverable 3.5) and an API surface breaking-change audit (Deliverable 3.8). Today the GTK adapter is not production-ready (missing key validation and DevTools wiring), and the test stack shows xUnit v3 version drift across repo/template/sample projects, increasing maintenance and CI risk.

This change advances Phase 3 readiness and improves developer experience goals (E2) while preserving contract-driven testability (G4).

## What Changes

- Make the GTK/WebKitGTK adapter production-ready for Phase 3 smoke validation, including DevTools open/close wiring where supported and explicit documented behavior where platform APIs are unavailable.
- Add/strengthen Linux runtime automation smoke validation for core scenarios relevant to GA readiness.
- Align xUnit v3 package versions across repo test projects, template tests, and samples (keeping runner compatibility).
- Execute the API surface review: produce a public API inventory, classify breaking changes, and publish a prioritized remediation plan.
- Spec hygiene: replace TBD Purpose placeholders in existing specs with concise purpose statements (no behavioral requirement changes).

## Non-goals

- No new WebView features or bridge capabilities.
- No performance benchmark work.
- No documentation expansion beyond spec Purpose cleanup.
- No platform parity work outside GTK/Linux readiness.

## Capabilities

### New Capabilities

- `gtk-adapter-production-readiness`: Acceptance criteria and validation lane for Linux/WebKitGTK adapter readiness in Phase 3 (DevTools behavior, smoke scenarios, CI expectations).

### Modified Capabilities

- `webview-testing-harness`: Require consistent xUnit v3 package versions across repo tests/templates/samples to avoid version drift.
- `api-surface-review`: Clarify required outputs (inventory + breaking-change audit + action plan) and their storage location for traceability.

## Impact

- GTK adapter code: `src/Agibuild.Fulora.Adapters.Gtk`
- Linux CI and automation lanes in Nuke `Ci` pipeline
- Test project dependencies (xUnit v3 packages and runner)
- Specs: purpose cleanup in `openspec/specs/` (documentation-only) and new/updated delta specs under this change
