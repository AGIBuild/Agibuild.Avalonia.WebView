## 1. Async Boundary Hardening (D1, D2, D6)

- [x] 1.1 Add `TryGetWebViewHandleAsync` to contract/runtime/control surfaces and wire adapter-backed execution to UI dispatch (Deliverable: D1, AC: off-thread calls complete without deadlock and return `Task<IPlatformHandle?>`).
- [x] 1.2 Keep `TryGetWebViewHandle` as constrained compatibility wrapper with explicit documentation and no new blocking call sites (Deliverable: D1, AC: whitelist tests unchanged or reduced and no new non-allowlisted sync waits).
- [x] 1.3 Refactor `AvaloniaWebDialog` option handling to instance-scoped propagation and remove global `WebViewEnvironment.Options` mutation (Deliverable: D2, AC: parallel dialog tests show isolated options with no cross-instance bleed).
- [x] 1.4 Enforce `[JsImport]` Task/Task<T>-only return contract across runtime proxy path and update exception messages consistently (Deliverable: D6, AC: sync-return interfaces fail deterministically with `NotSupportedException`).

## 2. Event Lifecycle Determinism (D3)

- [x] 2.1 Implement pre-attach subscription buffering and attach-time replay for `ContextMenuRequested` (Deliverable: D3, AC: subscribe-before-attach handlers fire after attach).
- [x] 2.2 Implement pre-attach unsubscription handling to prevent stale handler binding (Deliverable: D3, AC: subscribe-then-unsubscribe-before-attach results in no invocation).
- [x] 2.3 Add lifecycle-focused contract tests for pre-attach subscribe/unsubscribe and post-detach behavior (Deliverable: D3, AC: tests fail on regression and pass on all supported OS test runs).

## 3. Test Harness Determinism Upgrade (D4)

- [x] 3.1 Extract shared testing helpers for off-thread execution and dispatcher pumping into `Agibuild.Avalonia.WebView.Testing` (Deliverable: D4, AC: duplicated helper code in ContractSemantics suites is removed).
- [x] 3.2 Replace `Thread.Sleep` waits in prioritized unit files (`RuntimeCoverageTests`, `CoverageGapTests`, `RpcIntegrationTests`) with `DispatcherTestPump.WaitUntil` or `TaskCompletionSource`-driven waits (Deliverable: D4, AC: no remaining `Thread.Sleep` in migrated scopes).
- [x] 3.3 Replace `Thread.Sleep` waits in automation integration tests with condition-driven waits and preserve existing assertions (Deliverable: D4, AC: automation tests complete without timing-flaky failures).

## 4. Blocking-Wait Governance and Surface Review (D5 + API Audit)

- [x] 4.1 Extend production `GetAwaiter().GetResult()` guard to require explicit rationale metadata per allowlisted location (Deliverable: D5, AC: governance test reports missing rationale and blocks merge).
- [x] 4.2 Add a test-side guard that flags direct blocking waits outside approved helper boundaries (Deliverable: blocking-wait-governance, AC: direct test-body blocking waits are detected with actionable diagnostics).
- [x] 4.3 Update API surface review artifact/checklist with async-boundary status, pre-attach event semantics verification, and blocking-wait audit ownership (Deliverable: api-surface-review, AC: review output contains all three sections with pass/fail evidence).

## 5. Verification and Completion

- [x] 5.1 Run full unit tests and automation integration tests on Windows, Linux, and macOS targets used by CI (Deliverable: roadmap-3.8 hardening closure, AC: all suites pass with no new flaky retries).
- [x] 5.2 Run `openspec validate refactor-async-boundaries-and-test-stability --strict` and resolve all validation issues (Deliverable: change readiness, AC: strict validation passes cleanly).
