## Why

Long-running C# operations exposed via `[JsExport]` cannot be cancelled from JavaScript. When a user navigates away or cancels an action, the C# handler continues executing, wasting resources. CancellationToken is the standard .NET cancellation pattern and is the most requested V1 exclusion to lift. This enables real-world scenarios like cancellable search, file uploads, and LLM streaming.

**Goal alignment**: G1 (Type-Safe Bridge) — completing the bridge's async contract.  
**Roadmap alignment**: Phase 8 M8.2 — depends on M8.1 diagnostic infrastructure.

## What Changes

- **RPC protocol extension**: Add `$/cancelRequest` message type. When JS sends `{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"<request-id>"}}`, C# cancels the corresponding in-progress handler via CancellationTokenSource.
- **Generator update**: Remove AGBR004 diagnostic. BridgeHostEmitter generates handlers that create a CTS per request and pass the token to methods with CancellationToken parameters. The CancellationToken parameter is excluded from the RPC params object.
- **JS stub update**: Generated stubs accept an optional `options` parameter with `signal?: AbortSignal`. When the signal fires, the stub sends `$/cancelRequest`.
- **TypeScript update**: Methods with CancellationToken emit `options?: { signal?: AbortSignal }` as the last parameter in the TypeScript declaration.

## Capabilities

### New Capabilities

- `bridge-cancellation-support`: CancellationToken parameter support in bridge interfaces, with JS AbortSignal mapping and `$/cancelRequest` protocol extension.

### Modified Capabilities

- `bridge-v1-boundary-diagnostics`: Remove AGBR004 (CancellationToken) from the blocked patterns list.
- `bridge-contracts`: Bridge methods may now include a CancellationToken parameter that is excluded from the serialized params.
- `bridge-typescript-generation`: CancellationToken maps to `options?: { signal?: AbortSignal }` in TypeScript declarations.
- `js-csharp-rpc`: Add `$/cancelRequest` notification handling.

## Impact

- **Code**: `WebViewRpcService.cs` (cancel handling), `BridgeHostEmitter.cs` (CTS per request), `BridgeProxyEmitter.cs` (no change for now), `TypeScriptEmitter.cs` (AbortSignal mapping), `ModelExtractor.cs` (mark CancellationToken params), `BridgeDiagnostics.cs` (remove AGBR004)
- **Protocol**: New `$/cancelRequest` notification (backward compatible — old clients ignore it)
- **Tests**: Generator tests for CancellationToken methods, RPC cancellation tests, TypeScript mapping tests
- **Breaking**: None — additive change

## Non-goals

- C#→JS cancellation (C# cancels a JS operation) — deferred
- Timeout configuration per method — can be done by user with CTS
- Automatic cancellation on navigation — separate concern
