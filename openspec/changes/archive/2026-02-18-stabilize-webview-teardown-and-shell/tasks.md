## 1. WebView2 teardown stability (Phase 3 quality gate)

- [x] 1.1 Add targeted integration test coverage that reproduces rapid Attach/Detach/shutdown on Windows and asserts the smoke lane teardown guardrail markers do not appear
- [x] 1.2 Instrument the Windows adapter teardown path with structured diagnostics (behind an env flag) to capture ordering and thread context for teardown operations
- [x] 1.3 Refactor Windows adapter teardown into a single serialized lifecycle path with bounded cross-thread coordination (no deadlocks, deterministic event unsubscription, deterministic COM release ordering)
- [x] 1.4 Ensure parent window subclassing is always restored during teardown (including init-failed and detach-during-init paths)
- [x] 1.5 Ensure queued operations and readiness waits are deterministically canceled/faulted on detach and cannot run after teardown begins
- [x] 1.6 Run `NugetPackageTest` end-to-end and confirm it passes reliably on Windows without retries caused by teardown regressions

## 2. Shell experience (opt-in policies)

- [x] 2.1 Define public opt-in API surface for shell experience (policy objects / callbacks) in a UI-agnostic way and align it with existing WebViewCore events
- [x] 2.2 Implement NewWindowRequested policy strategies (navigate-in-place, delegate-to-host) and add contract tests using MockAdapter + deterministic dispatcher
- [x] 2.3 Implement download governance hook (set path / cancel) and add contract tests covering event ordering and determinism
- [x] 2.4 Implement permission governance hook (allow/deny) and add contract tests covering determinism and UI-thread execution

## 3. Regression coverage and quality gates

- [x] 3.1 Add a focused contract test suite for “no events after detach/dispose” semantics as they relate to shell policies and runtime wiring
- [x] 3.2 Add CI-friendly timeouts and diagnostics collection for teardown-related tests to avoid hangs and improve debuggability
