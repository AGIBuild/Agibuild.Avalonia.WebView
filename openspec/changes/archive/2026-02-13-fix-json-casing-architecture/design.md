## Context

The `WebViewRpcService` is the single serialization gateway for all C#↔JS bridge communication. It currently has three `JsonSerializer` call sites that use **default options** (PascalCase), while the JS→C# deserialization path (`RuntimeBridgeService`, `BridgeGeneratedJsonOptions`) already uses `CamelCase` + `CaseInsensitive`. This asymmetry forces every model to carry `[JsonPropertyName]` on each property — a DRY violation and ongoing maintenance burden.

Current serialization call sites in `WebViewRpcService`:

| Line | Call | Direction | Current Options |
|------|------|-----------|----------------|
| ~65 | `JsonSerializer.SerializeToElement(args)` | C#→JS (proxy call params) | Default (PascalCase) |
| ~191 | `JsonSerializer.SerializeToElement(result)` | C#→JS (handler response) | Default (PascalCase) |
| ~93 | `result.Deserialize<T>()` | JS→C# (proxy call result) | Default (PascalCase) |

The RPC envelope (`RpcRequest`, `RpcResponse`, `RpcErrorResponse`) is serialized via source-generated `RpcJsonContext` with explicit `[JsonPropertyName]`, so it is unaffected by this change.

## Goals / Non-Goals

**Goals:**
- Fix the C#→JS serialization asymmetry at the architecture level (single point of change)
- Make `[JsonPropertyName]` optional for standard camelCase property mapping
- Maintain backward compatibility — existing `[JsonPropertyName]` attributes continue to take priority
- Align with Phase 3 (G1 polish): "API surface review + breaking change audit"

**Non-Goals:**
- Making `JsonSerializerOptions` user-configurable or injectable
- Changing `BridgeGeneratedJsonOptions` or `RuntimeBridgeService.JsonOptions` (already correct)
- Modifying the RPC envelope serialization (`RpcJsonContext`)
- Changing the TypeScript generation pipeline

## Decisions

### D1: Static `JsonSerializerOptions` field on `WebViewRpcService`

**Choice**: Add a `private static readonly JsonSerializerOptions` field with `CamelCase` + `CaseInsensitive` to `WebViewRpcService`, used by all three call sites.

**Alternatives considered**:
- *Constructor-injected options*: Over-engineering — the bridge always talks to JS, camelCase is the only sensible default. Adds API surface without value.
- *Shared options class in Core*: Core is a contracts-only assembly; adding runtime `JsonSerializerOptions` there couples contracts to serialization config. The Generator already emits its own copy.
- *Central static class in Runtime*: Possible, but `WebViewRpcService` is the only consumer for outbound serialization. A shared class would exist solely for DRY with `RuntimeBridgeService.JsonOptions` — marginal benefit for 2 lines of config.

**Rationale**: Minimal change, maximum impact. The field is `static readonly` (thread-safe, allocated once). `System.Text.Json` caches converters internally, so reuse is performant.

### D2: Remove `[JsonPropertyName]` from sample models

**Choice**: Remove `[JsonPropertyName("...")]` from all sample model properties where the attribute value is simply the camelCase of the property name.

**Rationale**: Demonstrates the architecture fix works. Reduces boilerplate in the sample that serves as a reference for users. Properties where the JSON name differs from camelCase convention (if any) would retain the attribute.

### D3: Do not consolidate with `RuntimeBridgeService.JsonOptions`

**Choice**: Keep `RuntimeBridgeService.JsonOptions` and the new `WebViewRpcService` options as separate instances.

**Rationale**: They serve different layers (bridge handler dispatch vs. RPC transport). Coupling them adds a dependency arrow without benefit — both are 2-line identical configs. `System.Text.Json` caches by options instance, so separate instances have negligible cost.

## Testing Strategy

- **Existing CT**: `WebViewRpcServiceTests` already test RPC round-trips. Models without `[JsonPropertyName]` will now serialize as camelCase — tests should pass or be updated to verify camelCase output.
- **Existing IT**: Integration tests use real bridge calls through the full pipeline. If tests use models with `[JsonPropertyName]`, they are unaffected. If any test depends on PascalCase output, it was already broken in practice (JS expects camelCase).
- **New CT**: Add a test verifying that a plain C# record (no `[JsonPropertyName]`) round-trips correctly through `WebViewRpcService` with camelCase property names.

## Risks / Trade-offs

- **[Low] Behavioral change for models without `[JsonPropertyName]`** → If any consumer relies on PascalCase JSON from the bridge (unlikely — JS side always expects camelCase), this is a breaking change for them. Mitigation: this is the intended fix; the previous behavior was the bug.
- **[Low] Two identical `JsonSerializerOptions` instances** → Marginal memory overhead (two cached converter sets). Trade-off accepted for cleaner separation of concerns.
