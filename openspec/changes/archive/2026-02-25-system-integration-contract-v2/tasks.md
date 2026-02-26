## 1. System Action Whitelist v2 Contract (Deliverable 5.2)

- [x] 1.1 Extend typed system action contract to include `ShowAbout` and explicit whitelist v2 mapping (Acceptance: contracts compile; unknown action path returns deterministic deny taxonomy).
- [x] 1.2 Enforce whitelist-first evaluation before policy/provider execution in runtime flow (Acceptance: non-whitelisted action has provider execution count = 0 in CT).
- [x] 1.3 Standardize deny/failure taxonomy for whitelist and policy branches (Acceptance: diagnostics/tests assert stable reason codes for allow/deny/failure).

## 2. Tray Payload v2 Schema Governance (Deliverables 5.2 + 5.3)

- [x] 2.1 Define tray payload v2 schema with required core fields and bounded `extensions` map (Acceptance: schema validator rejects missing required fields).
- [x] 2.2 Add extension-key governance rule (`platform.*` namespace + bounded size) before dispatch (Acceptance: disallowed extension key blocks dispatch deterministically).
- [x] 2.3 Route schema validation outcome into structured diagnostics (Acceptance: tests assert operation/correlation/outcome/deny code stability).

## 3. Bridge & Shell Runtime Integration (Deliverables 5.2 + 5.3)

- [x] 3.1 Update `WebViewHostCapabilityBridge` for v2 tray schema validation and deny/failure isolation (Acceptance: inbound failure does not break unrelated operations).
- [x] 3.2 Update `WebViewShellExperience` for v2 evaluation order: schema/whitelist -> policy -> provider (Acceptance: tests validate strict order and zero bypass).
- [x] 3.3 Ensure shell domain isolation remains deterministic after v2 failures (Acceptance: permission/download/new-window flows unaffected in IT/CT).

## 4. Template App-shell v2 Flow (Deliverable 5.4)

- [x] 4.1 Update app-shell typed bridge service to expose v2 tray payload consumption markers (Acceptance: governance test finds required v2 markers).
- [x] 4.2 Update web demo to show `ShowAbout` allow/deny outcome rendering and v2 tray extension fields (Acceptance: deterministic UI assertions for both branches).
- [x] 4.3 Keep baseline preset free from v2 app-shell-only wiring (Acceptance: governance test confirms absence of v2 markers in baseline).

## 5. Test Matrix and Governance Hardening (Deliverables 5.3 + 5.5)

- [x] 5.1 Add CT matrix rows for `ShowAbout` whitelist and tray payload v2 schema branches (Acceptance: matrix includes allow/deny/failure for both rows).
- [x] 5.2 Add integration automation scenario for tray payload v2 roundtrip with extensions (Acceptance: automation lane passes with machine-checkable assertions).
- [x] 5.3 Add governance checks for direct platform payload passthrough bypass markers (Acceptance: CI fails on bypass marker regression).

## 6. Evidence and Release Readiness (Deliverable 5.5)

- [x] 6.1 Produce verification evidence mapping KPI -> implementation -> tests for v2 increment (Acceptance: evidence table includes pass/fail outcomes).
- [x] 6.2 Update roadmap/change notes with in-scope and explicit out-of-scope boundaries (Acceptance: notes explicitly exclude bundled-browser full API parity).
- [x] 6.3 Prepare archive-ready checklist with unresolved questions/owners/decision gates (Acceptance: open questions list owner + decision gate).
