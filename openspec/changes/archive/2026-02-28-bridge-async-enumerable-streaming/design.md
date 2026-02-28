## Context

The bridge currently supports strict request-response: one JSON-RPC request yields exactly one response. IAsyncEnumerable requires a protocol that supports multiple values over time. The plan recommends the AsyncIterator (Pull) model, which aligns with StreamJsonRpc's proven protocol and provides built-in backpressure.

## Goals / Non-Goals

**Goals:**
- C# `IAsyncEnumerable<T>` methods are consumable from JS via `for await...of`
- Protocol supports prefetch for performance (first N items in initial response)
- Enumeration lifecycle is explicit: iterator creation, pull, completion, and abort
- CancellationToken integration (abort triggers enumeration cancellation)

**Non-Goals:**
- Push-based streaming (Subscribe/Observable)
- Batching optimization (deferred to future iteration)
- C#→JS direction IAsyncEnumerable (only JS consuming C#)

## Decisions

### D1: Enumerator lifecycle managed by RPC service

**Choice**: `WebViewRpcService` tracks active enumerators in `ConcurrentDictionary<string, IAsyncDisposable>` (the enumerator). When a streaming method is invoked:
1. The handler returns immediately with `{ token, values? }` 
2. RPC service stores the enumerator, registers `$/enumerator/next` and `$/enumerator/abort` handlers per token
3. On `$/enumerator/next`, pull next item and respond with `{ values, finished }`
4. On `$/enumerator/abort` or when finished, dispose the enumerator

**Rationale**: Lifecycle at the RPC layer keeps the service layer clean. The enumerator token is a GUID, avoiding collisions.

### D2: Streaming handler code generation

**Choice**: For methods returning `IAsyncEnumerable<T>`, BridgeHostEmitter generates a handler that:
1. Calls `impl.Method(params)` to get the `IAsyncEnumerable<T>`
2. Gets the `IAsyncEnumerator<T>` from it
3. Optionally prefetches first item (to reduce latency)
4. Returns `{ token: "...", values?: [...] }` as the initial response
5. Registers the enumerator with the RPC service for pull-based consumption

The CancellationToken (if present) is wired to abort enumeration.

### D3: JS AsyncIterator wrapper

**Choice**: The generated JS stub returns an object implementing `Symbol.asyncIterator`. Each `next()` call sends `$/enumerator/next` to C# and awaits the response. The `return()` method sends `$/enumerator/abort`.

### D4: ModelExtractor changes

**Choice**: Add `IsAsyncEnumerable` and `AsyncEnumerableInnerType` properties to `BridgeMethodModel`. `IsAsync` remains false for IAsyncEnumerable returns (it's not `Task`), and `HasReturnValue` is true with `InnerReturnTypeFullName` set to the inner type.

## Risks / Trade-offs

- **[Risk] Enumerator leak if JS never calls next/abort** → Add timeout disposal (30s inactivity). Log warning.
- **[Risk] Cross-thread safety of IAsyncEnumerator** → Enumerator is consumed serially (pull model enforces this).
- **[Risk] Protocol complexity** → Well-tested pattern from StreamJsonRpc; test coverage mitigates.

## Testing Strategy

- **CT (Generator)**: IAsyncEnumerable return accepted without AGBR005; generated handler creates enumerator token
- **CT (RPC)**: `$/enumerator/next` returns items; `$/enumerator/abort` disposes enumerator
- **CT (TypeScript)**: IAsyncEnumerable<T> maps to `AsyncIterable<T>`
- **CT (End-to-end)**: Expose service with streaming method, consume via simulated JS pull requests
