## 1. ShowAbout Allowlist Contract Hardening (Phase 5.2 / 5.5)

- [x] 1.1 Add typed `ShowAbout` allowlist contract and deterministic deny reason mapping in runtime shell system-integration flow (Acceptance: non-allowlisted `ShowAbout` request returns deterministic deny and provider execution count remains zero).
- [x] 1.2 Wire policy evaluation and diagnostics fields for allowlisted `ShowAbout` success path (Acceptance: allow path reports stable correlation/outcome fields in CT assertions).
- [x] 1.3 Add/refresh governance tests for explicit allowlist marker semantics in app-shell preset (Acceptance: governance test fails when implicit always-allow `ShowAbout` marker appears).

## 2. Tray Inbound Payload Boundary Model (Phase 5.3 / 5.5)

- [x] 2.1 Define typed semantic tray event fields plus bounded metadata envelope schema in bridge contracts (Acceptance: inbound contract tests reject out-of-schema metadata deterministically).
- [x] 2.2 Enforce payload-boundary validation before web dispatch in shell-governed inbound pipeline (Acceptance: invalid metadata payload never reaches web subscriber path).
- [x] 2.3 Extend diagnostics with payload-boundary stage markers and failure category taxonomy (Acceptance: CT asserts stable boundary stage/failure category fields).

## 3. Federated Menu Pruning with Permission Profiles (Phase 5.2 / 5.3)

- [x] 3.1 Implement deterministic federation order: profile decision -> shell policy decision -> effective state mutation (Acceptance: profile deny short-circuits downstream mutation and policy stage execution).
- [x] 3.2 Implement conflict precedence and repeatability semantics for equivalent federated inputs (Acceptance: repeated equivalent inputs produce identical pruned state and diagnostics).
- [x] 3.3 Add isolation guards so pruning federation failure does not break other shell domains (Acceptance: permission/download/new-window deterministic behavior remains intact after pruning failure).

## 4. Template App-shell Marker and Demo Alignment (Phase 5.4)

- [x] 4.1 Update app-shell preset markers for `ShowAbout` explicit allowlist wiring (Acceptance: template source includes explicit allowlist marker and omits implicit allow marker).
- [x] 4.2 Update web demo marker flow for bounded tray metadata consumption (Acceptance: governance assertions detect canonical semantic-field consumption and reject raw passthrough markers).
- [x] 4.3 Update app-shell menu demo marker flow to show federated pruning stage-attributable result (Acceptance: integration demo assertions validate federated result metadata presence).

## 5. CT/IT/Automation and Matrix Evidence (Phase 5.5)

- [x] 5.1 Extend shell system-integration CT matrix with rows for `ShowAbout` allow/deny and tray metadata boundary branches (Acceptance: matrix file includes machine-checkable rows mapped to test methods).
- [x] 5.2 Add/refresh unit tests for federated pruning order and boundary validation failure isolation (Acceptance: updated test suite passes with branch assertions for allow/deny/failure).
- [x] 5.3 Add/refresh integration automation scenario covering `ShowAbout` action, tray inbound boundary validation, and federated pruning determinism in one roundtrip flow (Acceptance: runtime automation lane passes with deterministic assertions).

## 6. Release Evidence and Handoff

- [x] 6.1 Produce verification evidence mapping KPI -> implementation -> commands -> outcomes for this increment (Acceptance: `verification-evidence.md` includes pass/fail outcomes and command references).
- [x] 6.2 Update roadmap note with scope boundary and non-goals for this increment (Acceptance: boundary note explicitly excludes Electron full API parity and fallback routing).
- [x] 6.3 Prepare archive checklist with remaining open questions and owners (Acceptance: unresolved items list owner and decision gate for next increment).
