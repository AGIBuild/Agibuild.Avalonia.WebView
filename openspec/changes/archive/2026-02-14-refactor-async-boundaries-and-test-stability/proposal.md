## Why

Recent fixes resolved UI freeze and async-surface regressions, but follow-up review found remaining risk pockets: selective sync bridges in runtime/adapter boundaries, global mutable environment options, event-subscription edge cases before control attach, and timing-sensitive tests (`Thread.Sleep`) that reduce CI determinism.  
This change hardens those boundaries to keep async-first guarantees credible and improve long-term maintainability.

This directly supports **G4 (Contract-Driven Testability)** and **G3 (Secure/robust by default operational behavior)**, and extends **Phase 3 / Deliverable 3.8 (API surface review + breaking change audit)** with final contract and test-stability closure.

## What Changes

- Refactor native-handle access to prefer async-safe semantics and remove avoidable sync blocking in public/runtime paths.
- Remove or isolate global mutable `WebViewEnvironment.Options` side effects from dialog/control construction paths.
- Fix pre-attach event subscription behavior (notably context menu wiring) to avoid silent listener loss.
- Replace timing-based waits in unit/integration automation tests with condition-driven pump/wait primitives.
- Introduce stricter governance for blocking waits (`GetAwaiter().GetResult`) in production code via explicit whitelist validation and policy tests.
- Consolidate duplicated threading test helpers (`RunOffThread`, `PumpUntil`) into shared testing utilities.
- **BREAKING**: tighten bridge import contract so `[JsImport]` methods must be `Task`/`Task<T>` only.

## Capabilities

### New Capabilities
- `blocking-wait-governance`: enforce and continuously validate allowed blocking-wait boundaries in production sources.

### Modified Capabilities
- `webview-core-contracts`: refine native handle and environment option semantics toward async-safe, instance-scoped behavior.
- `webview-contract-semantics-v1`: clarify and enforce subscription/lifecycle behavior for pre-attach event wiring and async boundary expectations.
- `bridge-contracts`: formalize Task-only return contract for `[JsImport]` methods.
- `webview-testing-harness`: strengthen deterministic wait strategy and shared threading helper abstractions.
- `api-surface-review`: extend surface hardening outcomes with boundary cleanup and anti-regression checks.

## Non-goals

- No new end-user feature area (no additional browser capabilities).
- No redesign of adapter feature matrix across platforms.
- No broad architecture rewrite of `WebViewCore` in a single step.
- No CI pipeline topology redesign beyond test determinism and categorization hooks.

## Impact

- Affected code: `WebViewCore`, `WebView`, `WebDialog`/`AvaloniaWebDialog`, selected platform adapters, bridge runtime, and test harness/utilities.
- API impact: stricter bridge import contract and async boundary clarifications.
- Test impact: unit + automation suites gain deterministic wait patterns and anti-regression guards.
- Dependency impact: none expected; changes are internal refactors and contract hardening.
