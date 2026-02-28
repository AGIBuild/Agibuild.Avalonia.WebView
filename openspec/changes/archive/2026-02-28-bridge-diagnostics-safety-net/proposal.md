## Why

The Bridge V1 source generator silently accepts unsupported patterns (generics, overloads, ref/out, CancellationToken, IAsyncEnumerable) and may produce invalid or broken code. The TypeScript emitter uses a naive comma-split for generic args that breaks on nested generics like `Dictionary<string, List<int>>`. `Remove<T>()` unregisters RPC handlers but leaves stale JS stubs on `window.agWebView.bridge`, which can cause confusing errors if the service is re-exposed or if JS calls the stale stub. These issues must be fixed before V2 feature work begins.

**Goal alignment**: G1 (Type-Safe Bridge) — making the bridge boundary explicit and safe.  
**Roadmap alignment**: Phase 8 M8.1 — prerequisite for all V2 bridge work (CancellationToken, IAsyncEnumerable, generics).

## What Changes

- **Generator diagnostics**: Emit Roslyn `DiagnosticSeverity.Error` when a `[JsExport]` or `[JsImport]` interface method uses V1-unsupported patterns:
  - Generic method parameters (`Method<T>(...)`)
  - Method overloads (same name, different parameters)
  - `ref` / `out` / `in` parameters
  - `CancellationToken` parameter (will be supported in M8.2, blocked until then)
  - `IAsyncEnumerable<T>` return type (will be supported in M8.3, blocked until then)
- **Nested generic TypeScript fix**: Replace `ExtractGenericArgs` simple comma-split with bracket-depth-aware parsing to correctly handle `Dictionary<string, List<int>>` → `Record<string, number[]>`.
- **JS stub cleanup on Remove<T>()**: Execute `delete window.agWebView.bridge.<ServiceName>` via `InvokeScriptAsync` when a service is removed.

## Capabilities

### New Capabilities

- `bridge-v1-boundary-diagnostics`: Roslyn analyzer diagnostics that enforce V1 scope boundaries at compile time, preventing silent generation of invalid bridge code.

### Modified Capabilities

- `bridge-contracts`: `Remove<T>()` must clean up JS-side stubs, not just C# RPC handlers.
- `bridge-typescript-generation`: TypeScript type mapping must handle nested generic types correctly.

## Impact

- **Code**: `ModelExtractor.cs` (validation), `WebViewBridgeGenerator.cs` (diagnostic reporting), `TypeScriptEmitter.cs` (generic parsing), `RuntimeBridgeService.cs` (Remove cleanup)
- **Tests**: New CT for each diagnostic, updated CT for nested generics, updated CT for Remove cleanup
- **API**: No public API changes; diagnostics are compile-time only
- **Breaking**: None — previously-broken patterns now produce compile errors instead of silently generating bad code

## Non-goals

- Adding support for any V1-excluded pattern (that's M8.2–M8.4)
- Changing the bridge wire protocol
- Modifying the JS RPC stub architecture
