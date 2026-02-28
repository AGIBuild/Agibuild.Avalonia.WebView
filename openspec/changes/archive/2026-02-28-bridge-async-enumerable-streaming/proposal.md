## Why

`IAsyncEnumerable<T>` is the standard .NET pattern for streaming data (LLM responses, log tailing, real-time metrics). Without bridge support, developers must implement ad-hoc streaming via multiple RPC calls or polling. Lifting this V1 exclusion enables natural streaming from C# to JS using `for await...of` syntax, which is critical for AI-powered application scenarios.

**Goal alignment**: G1 (Type-Safe Bridge) — completing the bridge's streaming contract.
**Roadmap alignment**: Phase 8 M8.3 — depends on M8.2 (CancellationToken infrastructure used for `$/enumerator/abort`).

## What Changes

- **Protocol extension (Pull model, AsyncIterator)**: Based on StreamJsonRpc protocol pattern:
  - Initial response returns `{ token: "<enum-id>", values?: [item1, ...] }` with optional prefetched items
  - `$/enumerator/next` request pulls next batch: `{ params: { token: "<enum-id>" } }` → `{ values: [...], finished: bool }`
  - `$/enumerator/abort` notification for early termination: `{ params: { token: "<enum-id>" } }`
- **Generator update**: Remove AGBR005 diagnostic. BridgeHostEmitter generates streaming handlers that iterate `IAsyncEnumerable<T>` and respond to pull requests. TypeScript emits `AsyncIterable<T>` return type.
- **JS runtime**: Add `AsyncIterable` wrapper in the RPC stub that implements the `Symbol.asyncIterator` protocol, sending `$/enumerator/next` on each `next()` call and `$/enumerator/abort` on `return()`.

## Capabilities

### New Capabilities

- `bridge-async-enumerable`: IAsyncEnumerable streaming protocol with pull-based enumerator, prefetch support, and JS AsyncIterator mapping.

### Modified Capabilities

- `bridge-v1-boundary-diagnostics`: Remove AGBR005 (IAsyncEnumerable) from blocked patterns.
- `bridge-typescript-generation`: IAsyncEnumerable<T> maps to `AsyncIterable<T>` in TypeScript.
- `js-csharp-rpc`: Add `$/enumerator/next` request and `$/enumerator/abort` notification handling.

## Impact

- **Code**: `WebViewRpcService.cs` (enumerator protocol), `BridgeHostEmitter.cs` (streaming handler), `ModelExtractor.cs` (detect IAsyncEnumerable), `TypeScriptEmitter.cs` (AsyncIterable mapping), `BridgeDiagnostics.cs` (remove AGBR005)
- **Protocol**: Three new message types (backward compatible)
- **JS**: New `AsyncIterable` wrapper class
- **Breaking**: None — additive change

## Non-goals

- Server-push (Subscribe/Observable) model — AsyncIterator chosen per plan decision
- Bidirectional streaming (JS→C# streaming)
- Backpressure configuration per method
