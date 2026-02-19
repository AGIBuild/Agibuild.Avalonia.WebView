## 1. Profile Contracts & API Surface (Deliverable 4.4)

- [x] 1.1 Define `WebViewSessionPermissionProfile` contract and identity model (Acceptance: profile contracts compile and remain platform-agnostic).
- [x] 1.2 Define profile resolver policy interface and evaluation context (root/parent/window/scope/request/permissionKind) (Acceptance: context carries all identity fields required by specs).
- [x] 1.3 Define typed decision/result models for session scope and permission state under profile governance (Acceptance: deterministic allow/deny/default semantics are representable without adapter-specific types).

## 2. Runtime Orchestration Wiring (Deliverable 4.4)

- [x] 2.1 Integrate profile resolution into shell session decision flow for root and managed child windows (Acceptance: root/child profile selection follows deterministic inheritance or override behavior).
- [x] 2.2 Integrate profile-driven permission decision path into shell permission handling (Acceptance: explicit profile decision applies before fallback semantics).
- [x] 2.3 Extend multi-window lifecycle metadata/diagnostics with profile identity correlation (Acceptance: lifecycle and profile outcomes can be correlated by stable window id).
- [x] 2.4 Implement failure isolation and policy error reporting for profile resolution/application errors (Acceptance: profile failures do not corrupt unrelated domains).

## 3. Contract Test Coverage (G3/G4, Deliverable 4.4)

- [x] 3.1 Add CT for profile resolution determinism across equivalent contexts (Acceptance: repeated evaluations produce identical profile outputs).
- [x] 3.2 Add CT for parent-child inheritance and override matrix (Acceptance: inherited and overridden profile outcomes are both covered).
- [x] 3.3 Add CT for permission precedence (profile-first) and fallback compatibility (Acceptance: explicit profile decision and no-decision fallback branches both pass).
- [x] 3.4 Add CT for profile failure isolation and diagnostic metadata (Acceptance: failure is reported and unrelated policy domains continue functioning).

## 4. Integration Validation (G3/G4, Deliverable 4.4)

- [x] 4.1 Add focused IT for representative multi-window flow with configured profiles (Acceptance: session isolation and permission outcomes match profile definitions end-to-end).
- [x] 4.2 Add stress IT for repeated profile-governed open/close + permission cycles (Acceptance: no stale profile-window correlation and deterministic pass criteria).

## 5. Milestone Exit Checks (M4.4)

- [x] 5.1 Run impacted unit/contract/integration suites and capture command outcomes (Acceptance: all impacted suites pass with reproducible command set).
- [x] 5.2 Produce requirements traceability for `webview-session-permission-profiles` and modified capabilities (`shell-session-policy`, `webview-shell-experience`, `webview-multi-window-lifecycle`) (Acceptance: each added/modified scenario maps to CT or IT evidence).
