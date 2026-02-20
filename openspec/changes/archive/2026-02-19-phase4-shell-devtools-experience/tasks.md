## 1. Shell DevTools Policy Contracts

- [x] 1.1 Add shell DevTools policy domain/contracts (action/context/decision/interface) and wire options surface in `WebViewShellExperienceOptions` (Acceptance: shell can evaluate allow/deny decisions for open/close/query actions).
- [x] 1.2 Add shell DevTools operation entry points with policy-first execution and explicit policy-failure reporting (Acceptance: denied operations are blocked deterministically and reported in `PolicyError` domain).

## 2. Test Coverage

- [x] 2.1 Add unit tests for allow/deny DevTools shell behavior and error reporting isolation (Acceptance: tests verify underlying operation execution only when allowed and proper deny failure metadata).
- [x] 2.2 Add automation integration coverage proving DevTools deny does not break permission governance flow (Acceptance: integration test validates deterministic cross-domain behavior after deny).

## 3. Exit Checks

- [x] 3.1 Run targeted and full impacted unit/integration suites (Acceptance: all relevant suites pass).
- [x] 3.2 Produce verification evidence and traceability for `webview-shell-experience` delta requirements (Acceptance: scenarios map to executable tests).
