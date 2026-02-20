## 1. Command Policy Runtime Integration

- [x] 1.1 Add shell command policy domain/contracts and options in `WebViewShellExperience` (Acceptance: policy can evaluate allow/deny for each `WebViewCommand` action).
- [x] 1.2 Implement `ExecuteCommandAsync(WebViewCommand command)` with policy-first execution and deterministic failure reporting (Acceptance: denied/missing-manager paths return false and report policy failure metadata).

## 2. Test Coverage

- [x] 2.1 Add unit tests for allowed command execution, denied command behavior, and missing command manager behavior (Acceptance: command manager invocation count and error domain assertions pass).
- [x] 2.2 Add automation integration test for command deny isolation against permission domain (Acceptance: permission governance remains deterministic after command denial).

## 3. Exit Checks

- [x] 3.1 Run targeted and full impacted unit/integration suites (Acceptance: all relevant suites pass).
- [x] 3.2 Produce verification evidence and requirement traceability for `webview-shell-experience` delta (Acceptance: each scenario maps to executable tests).
