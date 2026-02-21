## 1. Capability Contract Extension (Phase 5.1 / Extends 4.3)

- [x] 1.1 Add typed operation enums and DTO contracts for menu/tray/system-action in runtime shell contracts (Acceptance: contracts compile and preserve existing operation values).
- [x] 1.2 Extend provider/policy interfaces for new operations without breaking existing implementers (Acceptance: build passes with deterministic defaults for unconfigured operations).
- [x] 1.3 Add deterministic outcome and diagnostic contract coverage for new operation types (Acceptance: contract tests assert allow/deny/failure payload fields for each new operation).

## 2. Policy-first Runtime Wiring (Phase 5.2 / Extends 4.3)

- [x] 2.1 Implement capability bridge routing for menu/tray/system-action with policy-before-provider order (Acceptance: deny branch confirms provider invocation count is zero).
- [x] 2.2 Add failure isolation for system-integration provider/policy exceptions (Acceptance: failure category and error payload assertions are deterministic).
- [x] 2.3 Ensure no direct runtime bypass path exists outside bridge entry points (Acceptance: governance test fails when non-bridge execution marker is introduced).

## 3. Shell Experience Entry Points (Phase 5.2 / Extends 4.5)

- [x] 3.1 Add shell-governed entry APIs for menu/tray/system-action that delegate to host capability bridge (Acceptance: unconfigured bridge returns deterministic unavailable outcome).
- [x] 3.2 Keep shell behavior opt-in and non-breaking for existing domains (Acceptance: existing shell experience tests pass unchanged when new features are disabled).
- [x] 3.3 Add domain isolation assertions across permission/download/new-window vs system integration flows (Acceptance: failing system-integration path does not fail unrelated domain tests).

## 4. Template Web-first Flow (Phase 5.4 / Extends 4.5)

- [x] 4.1 Extend `app-shell` preset with one canonical typed desktop shell service for menu/tray/system-action operations (Acceptance: generated template contains expected service exposure markers).
- [x] 4.2 Add minimal web demo flow for invoking typed system integration operations (Acceptance: template automation validates web call -> typed result path deterministically).
- [x] 4.3 Keep baseline preset minimal with no shell system integration wiring (Acceptance: governance test asserts absence of app-shell-only markers in baseline output).

## 5. Test & Governance Hardening (Phase 5.3 / 5.5)

- [x] 5.1 Add CT matrix tests for all new operations across allow/deny/failure branches (Acceptance: matrix coverage report includes menu/tray/system-action rows).
- [x] 5.2 Add integration automation scenario for representative menu/tray flow with diagnostics assertions (Acceptance: automation lane passes with machine-checkable event payload assertions).
- [x] 5.3 Add schema stability governance tests for new diagnostic payload shape and capability contract compatibility (Acceptance: CI guard fails on contract drift).

## 6. Release Evidence & Rollout Readiness (Phase 5.5)

- [x] 6.1 Update verification evidence mapping to include system integration KPI linkage (Acceptance: each P0/P1 KPI has evidence row with pass/fail criteria).
- [x] 6.2 Update roadmap/change notes for this extension and cross-reference implementation boundaries (Acceptance: roadmap linkage and out-of-scope boundaries are explicit).
- [x] 6.3 Prepare archive-ready checklist and residual risk log for follow-up phases (Acceptance: open questions are either resolved or tracked with owners).
