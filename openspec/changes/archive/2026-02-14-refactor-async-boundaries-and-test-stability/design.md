## Context

`fix-ui-thread-dispatch` established async-first API behavior and a UI-actor execution model, but the codebase still contains a few contract edge cases that weaken that model in practice:

- Native handle retrieval still exposes a sync boundary in runtime (`TryGetWebViewHandle`).
- `AvaloniaWebDialog` mutates global `WebViewEnvironment.Options`, creating cross-instance side effects.
- Some control events can be subscribed before attach but silently dropped.
- Tests still rely on timing sleeps and duplicated threading helpers, reducing CI determinism.

This design is a Phase 3 hardening continuation of **ROADMAP 3.8 (API surface review + breaking change audit)** and directly advances **G4 (Contract-driven testability)** with additional support for **G3 (robust secure-by-default runtime behavior)**.

## Goals / Non-Goals

**Goals:**
- Remove avoidable sync-boundary ambiguity from runtime contracts.
- Eliminate global environment-option side effects from dialog/control construction.
- Guarantee event subscription behavior is deterministic across pre-attach and post-attach phases.
- Replace timing-based waits in tests with condition-driven synchronization primitives.
- Enforce explicit governance for blocking waits in production sources.
- Consolidate shared threading test helpers to reduce duplicate logic and drift.

**Non-Goals:**
- No broad redesign/split of `WebViewCore` in this change.
- No platform feature expansion (new browser capabilities).
- No adapter matrix behavior changes beyond boundary hardening.
- No CI pipeline architecture redesign (only test determinism and categorization hooks).

## Decisions

### D1 — Add async native-handle access path and de-emphasize sync retrieval

**Decision:** introduce `TryGetWebViewHandleAsync()` at runtime/control contract boundaries; keep sync `TryGetWebViewHandle()` as a constrained compatibility path (documented and test-guarded).

**Alternatives considered:**
- Remove sync API immediately: cleaner, but larger breaking surface for callers.
- Keep sync only: preserves ambiguity and hidden blocking.

**Why this choice:** progressive contract hardening with controlled migration risk.

### D2 — Remove global options mutation in dialog construction

**Decision:** replace `AvaloniaWebDialog` global `WebViewEnvironment.Options` mutation with instance-scoped option propagation (constructor/factory wiring).

**Alternatives considered:**
- Keep global mutation and restore previous value: still race-prone under concurrency.
- Keep status quo: unacceptable isolation risk.

**Why this choice:** instance-scoped behavior is required for deterministic multi-dialog/test scenarios.

### D3 — Guarantee pre-attach event subscription semantics

**Decision:** queue/pre-bind event handlers when `_core` is not yet created, then replay/bind on attach, including safe unbind handling.

**Alternatives considered:**
- Document “subscribe after attach”: low implementation cost but poor API ergonomics.
- Ignore pre-attach subscribers: current behavior and source of silent failures.

**Why this choice:** contracts should be deterministic and intuitive.

### D4 — Test synchronization standardization

**Decision:** replace `Thread.Sleep`-based waits with `DispatcherTestPump.WaitUntil` or `TaskCompletionSource` condition waits; centralize duplicated `RunOffThread`/`PumpUntil` into shared testing helpers.

**Alternatives considered:**
- Keep sleeps and increase durations: slower and still flaky.
- Introduce broad retry policy only: hides root causes.

**Why this choice:** deterministic synchronization is the most direct path to stable CI.

### D5 — Blocking-wait governance as executable policy

**Decision:** keep a strict source-level whitelist test for `GetAwaiter().GetResult()` in production code and require explicit audited reasons for allowed sites.

**Alternatives considered:**
- Style guidance only: unenforceable.
- Ban all blocking waits: currently impractical for a few platform callback bridges.

**Why this choice:** enforceable guardrail with pragmatic boundary exceptions.

### D6 — Bridge import contract hardening

**Decision:** `[JsImport]` methods must return `Task` or `Task<T>`; synchronous return methods throw deterministic `NotSupportedException`.

**Alternatives considered:**
- Keep sync return bridge path: requires blocking wait and weakens async-first model.

**Why this choice:** aligns runtime bridge behavior with async-only contract direction.

## Risks / Trade-offs

- **[Risk] Incremental breaking API impact (`TryGetWebViewHandleAsync`)** → **Mitigation:** keep sync compatibility path temporarily, add migration guidance and tests for both paths.
- **[Risk] Event replay implementation complexity** → **Mitigation:** add dedicated lifecycle/event wiring contract tests (subscribe-before-attach, unsubscribe-before-attach, post-detach behavior).
- **[Risk] Refactoring tests may mask behavioral regressions** → **Mitigation:** preserve assertions and only replace waiting mechanisms; run full CT + automation suites.
- **[Risk] Remaining allowed blocking waits may expand over time** → **Mitigation:** whitelist guard test with exact location matching and CI enforcement.

## Migration Plan

1. Add/propagate async handle retrieval API and wire runtime/control implementations.
2. Refactor `AvaloniaWebDialog` option wiring to instance scope; remove global mutation.
3. Implement pending-subscriber buffering for pre-attach events and bind-on-attach replay.
4. Extract shared contract-test threading helpers and migrate duplicated implementations.
5. Replace high-impact sleep-based waits in unit/automation tests with condition-driven waits.
6. Update/expand blocking-wait whitelist tests and bridge sync-return contract tests.
7. Run full unit tests + automation integration tests; address any deterministic failures.

## Testing Strategy

- **Contract Tests (CT):**
  - `TryGetWebViewHandleAsync` any-thread and lifecycle semantics.
  - pre-attach subscription semantics for context menu and selected forwarded events.
  - blocking-wait whitelist guard in `src/`.
  - `[JsImport]` sync-return rejection behavior.

- **Integration Tests (IT / Automation):**
  - migrate sleep-based waits to condition-driven waits in RPC/bridge and dialog navigation scenarios.
  - verify no regressions in WebDialog and Feature/Consumer automation flows.

- **MockBridge / Testing Harness:**
  - centralize and reuse threading helpers (`RunOffThread`, `PumpUntil`) in `Agibuild.Fulora.Testing`.
  - ensure helper behavior remains deterministic and timeout-bounded.

## Open Questions

- Should sync `TryGetWebViewHandle()` remain permanently or be fully removed in next breaking window?
- Which additional event surfaces should adopt pre-attach buffering beyond context menu for v1 hardening?
- Do we enforce test trait categorization (`RequiresNetwork`, `E2E`) in this change or defer to a follow-up QA governance change?
