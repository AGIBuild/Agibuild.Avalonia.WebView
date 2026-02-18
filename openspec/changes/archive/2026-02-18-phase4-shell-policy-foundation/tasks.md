## 1. Contracts & API Surface (Deliverable 4.1)

- [x] 1.1 Introduce shell session policy contracts (`shared`/`isolated` scope + scope identity) in runtime abstractions (Acceptance: contracts compile and remain platform-agnostic).
- [x] 1.2 Extend shell policy option model to represent deterministic policy domains (new-window/download/permission/session) with explicit opt-in semantics (Acceptance: no behavior change when options are not configured).
- [x] 1.3 Define policy failure reporting path for shell handlers (Acceptance: handler exceptions map to existing runtime error flow without process crash).

## 2. Runtime Wiring & Semantics (Deliverable 4.1)

- [x] 2.1 Implement deterministic shell policy execution order in runtime event pipeline (Acceptance: order is fixed and covered by contract tests).
- [x] 2.2 Implement domain fallback semantics when handlers are absent or defer (Acceptance: baseline behavior remains unchanged and verified by tests).
- [x] 2.3 Wire session policy resolution into shell-governed WebView context creation path (Acceptance: identical input policy produces identical scope decision).

## 3. Contract Tests (G4, Deliverable 4.1)

- [x] 3.1 Add CT coverage for opt-in/non-breaking semantics across all shell policy domains (Acceptance: tests verify disabled shell policy keeps baseline behavior).
- [x] 3.2 Add CT coverage for deterministic order + fallback behavior (Acceptance: tests assert policy-before-fallback and stable fallback outputs).
- [x] 3.3 Add CT coverage for policy failure isolation (Acceptance: exception in one handler does not break unrelated policy domains).
- [x] 3.4 Add CT coverage for session policy determinism and scope identity propagation (Acceptance: repeated evaluations return stable decisions).

## 4. Integration Validation (G3/G4, Deliverable 4.1)

- [x] 4.1 Add focused IT flow validating shell policy behavior on desktop runtime (Acceptance: one representative end-to-end flow passes in CI lane).
- [x] 4.2 Add stress-oriented shell policy scenario for handler/fallback lifecycle stability (Acceptance: repeated run has deterministic pass criteria and no teardown regressions).

## 5. Milestone Exit Checks (M4.1)

- [x] 5.1 Run relevant unit/contract/integration test suites and record outcomes in change notes (Acceptance: all impacted suites pass locally/CI).
- [x] 5.2 Validate requirements-to-tests traceability for `webview-shell-experience` and `shell-session-policy` (Acceptance: every added/modified scenario has a mapped test).
