## Why

WebView thread-safety currently depends on callers invoking many APIs on the UI thread. This leaked threading details to consumers and caused E2E failures on Windows (Screenshot, PrintToPdf, FindInPage, PreloadScript) when calls resumed on background threads after `ConfigureAwait(false)`.

The previous design still mixed sync/async APIs and multiple dispatch patterns, which left gaps (manager/facade bypass paths, undefined concurrency semantics, and non-falsifiable reliability claims). Since this project has no compatibility burden, we can make a clean contract reset: **fully async public APIs + a single UI-thread actor pipeline**.

This change aligns with Phase 3 `3.8 API surface review + breaking change audit` and advances F4 (rich feature reliability) and G4 (contract-driven testability).

**Goal alignment**: F4 (Rich feature set reliability), Phase 3.8 (API surface review)

## What Changes

- **BREAKING** Move WebView public command/property operations to async APIs (`*Async`) so callers never depend on UI-thread affinity.
- Introduce a single-operation execution pipeline in `WebViewCore` (`OperationQueue` + single consumer + UI dispatcher hop).
- Enforce strict lifecycle state machine (`Created -> Attaching -> Ready -> Detaching -> Disposed`) with deterministic acceptance/rejection rules.
- Close all bypass paths: manager/facade operations must execute through the same operation pipeline.
- Define uniform failure taxonomy (`Disposed`, `NotReady`, `DispatchFailed`, `AdapterFailed`) and mandatory operation-level diagnostics.
- Keep `IWebViewDispatcher` async-only (no sync blocking API surface).

## Capabilities

### New Capabilities

_None_

### Modified Capabilities

- `webview-core-contracts`: public contract surface becomes async-first and thread-neutral for adapter-backed operations.
- `webview-contract-semantics-v1`: API threading model changes from “sync API requires UI thread” to “all public operations are queue-dispatched and callable from any thread”.
- `command-manager`: command execution API changes to async.
- `zoom-control`: zoom read/write API changes from sync property to async operations.
- `devtools-toggle`: DevTools surface changes to async operations.
- `preload-script`: preload add/remove API changes to async operations.
- `find-in-page`: stop-find API changes to async operation.

## Impact

- **Breaking API reset** in Core/WebView public surface: synchronous command/property APIs are replaced by async APIs.
- `WebViewCore` internals are restructured around operation queue + lifecycle gating + deterministic dispatch.
- Adapter contracts remain focused on platform capabilities; thread marshaling responsibility is centralized in `WebViewCore`.
- Test scope expands to include operation-order invariants, lifecycle rejection behavior, and non-UI-thread stress scenarios.
- E2E must validate both thread-error elimination and Runtime-version behavior for PrintToPdf.
