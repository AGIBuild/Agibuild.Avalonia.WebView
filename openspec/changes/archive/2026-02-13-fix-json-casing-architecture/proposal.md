## Why

`WebViewRpcService` — the single serialization gateway for C#→JS data — uses `JsonSerializer.SerializeToElement()` with **default options** (PascalCase). This forces every model to manually annotate `[JsonPropertyName("camelCase")]` on every property, violating DRY and the project's "contract-first, convention-over-configuration" principle. The JS→C# direction already uses `CamelCase` policy correctly (`BridgeGeneratedJsonOptions`, `RuntimeBridgeService.JsonOptions`), creating an asymmetric architecture. This is a Phase 3 polish fix aligned with **G1** (Type-Safe Bridge) quality — the bridge should serialize correctly by convention, not by per-property annotation.

## What Changes

- **Add shared `JsonSerializerOptions` (camelCase + case-insensitive) to `WebViewRpcService`** and use it in all three serialization call sites: `SerializeToElement(args)`, `SerializeToElement(result)`, `result.Deserialize<T>()`
- **Remove redundant `[JsonPropertyName]` annotations** from sample models — they become optional (retained attributes still take priority, so removal is non-breaking)
- No new APIs, no breaking changes, no new dependencies

## Non-goals

- Changing the RPC envelope format (`RpcRequest`/`RpcResponse` already use source-generated `RpcJsonContext` with explicit `[JsonPropertyName]` — unaffected)
- Making the naming policy user-configurable (JS always expects camelCase; configurability adds complexity without value)
- Modifying `BridgeGeneratedJsonOptions` or `RuntimeBridgeService.JsonOptions` (they already work correctly)

## Capabilities

### New Capabilities

_None._

### Modified Capabilities

- `js-csharp-rpc`: The RPC service SHALL use `CamelCase` naming policy for serializing params and results in both directions, making `[JsonPropertyName]` optional for standard camelCase mapping.

## Impact

- **Code**: `WebViewRpcService.cs` (3 call sites), sample model files (attribute removal)
- **APIs**: No public API changes
- **Behavior**: C#→JS payloads now serialize as camelCase by convention; models with `[JsonPropertyName]` are unaffected (attribute takes priority)
- **Tests**: Existing tests should pass; models without `[JsonPropertyName]` that previously relied on PascalCase will now serialize as camelCase (this is the intended fix)
