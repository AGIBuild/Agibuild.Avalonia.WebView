## Context

The bridge currently treats all method calls as fire-and-forget from a cancellation perspective. The RPC layer (`WebViewRpcService`) dispatches incoming JS→C# requests and awaits the handler. There is no mechanism for the caller to cancel an in-progress handler. CancellationToken parameters in bridge interfaces are blocked by AGBR004.

## Goals / Non-Goals

**Goals:**
- JS callers can cancel in-progress C# bridge method calls via AbortSignal
- CancellationToken parameters are seamlessly supported in `[JsExport]` methods
- The protocol extension (`$/cancelRequest`) is backward compatible

**Non-Goals:**
- C#→JS cancellation (the reverse direction)
- Automatic timeout policies
- Stream cancellation (that's M8.3)

## Decisions

### D1: Cancellation request as JSON-RPC notification

**Choice**: `$/cancelRequest` is a JSON-RPC notification (no `id` field, no response expected).

```json
{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"__js_42"}}
```

**Rationale**: Cancellation is best-effort. The caller doesn't need confirmation — it will get the normal response (either result or error) for the original request. StreamJsonRpc and LSP both use this pattern.

### D2: CTS tracking in RPC service

**Choice**: Add `ConcurrentDictionary<string, CancellationTokenSource> _activeCancellations` to `WebViewRpcService`. When dispatching a request that has a cancellation-aware handler, register the CTS. When `$/cancelRequest` arrives, look up and cancel.

**Alternative**: Track in `RuntimeBridgeService` — rejected because cancellation is a transport-level concern, not a service-level concern.

### D3: CancellationToken parameter handling in source generator

**Choice**: In `ModelExtractor`, mark CancellationToken parameters with a flag (`IsCancellationToken = true`). In `BridgeHostEmitter`, generate handler code that:
1. Creates a `CancellationTokenSource` for the request
2. Passes `cts.Token` to the CancellationToken parameter
3. Registers the CTS with the RPC service for cancellation
4. Disposes the CTS after the handler completes

The CancellationToken parameter is excluded from the JSON params deserialization.

### D4: JS-side AbortSignal integration

**Choice**: Generated JS stubs accept an optional `options` object as the last argument:

```javascript
appService.longOperation(query, { signal: abortController.signal })
```

When the signal fires `abort`, the stub sends `$/cancelRequest` with the request ID. The stub's `invoke` function is extended to accept the signal.

### D5: RPC _dispatch handling for notifications

**Choice**: Extend `TryProcessMessage` to handle messages without `id` that have a `method` field. Currently, messages without `id` fall through. Add a check: if `method` is `$/cancelRequest`, extract the target request ID from params and cancel.

## Risks / Trade-offs

- **[Risk] Cancel arrives after handler completes** → CTS lookup returns null; gracefully ignored. No harm.
- **[Risk] Cancel arrives before handler starts** → CTS is cancelled before the handler reads the token; handler throws OperationCanceledException immediately. RPC layer catches and returns error code -32800.
- **[Risk] Handler ignores CancellationToken** → Token is passed but not observed; cancellation has no effect. This is standard .NET behavior.

## Testing Strategy

- **CT (Generator)**: CancellationToken parameter is accepted without AGBR004 diagnostic; generated handler creates CTS and passes token
- **CT (RPC)**: `$/cancelRequest` message cancels pending handler; cancelled handler returns error -32800
- **CT (TypeScript)**: CancellationToken maps to `options?: { signal?: AbortSignal }` in .d.ts output
- **CT (JS stub)**: Generated stub includes signal handling code
