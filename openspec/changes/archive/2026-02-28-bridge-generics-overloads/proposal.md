## Why

Bridge V1 blocks method overloads (AGBR002) and generic methods (AGBR001), limiting API expressiveness. Method overloads are a natural C# pattern that CAN be supported via parameter-count disambiguation. Generic methods and open generic interfaces CANNOT be supported in a source-generator-only approach over JSON-RPC (TypeScript generics are erased at runtime; the generated C# handler needs a concrete T that the generator cannot know). The diagnostics for these unsupported patterns need clearer messages with actionable alternatives.

**Goal alignment**: G1 (Type-Safe Bridge) — increasing API expressiveness while maintaining compile-time safety.
**ROADMAP**: Phase 8 M8.4 — Bridge V2 Generics & Overloads.

## What Changes

- **Method overloads**: Remove AGBR002 when overloads have distinct parameter counts; generate parameter-count-discriminated RPC method names (`Service.method` for fewest params, `Service.method$N` for others); generate JavaScript dispatcher function; generate TypeScript overloaded signatures
- **Open generic interface diagnostic**: Add AGBR006 error for `[JsExport]/[JsImport]` interfaces with open type parameters (e.g., `IRepository<T>`) — these produce invalid generated code
- **Improved AGBR001 message**: Suggest concrete methods or generic interfaces as alternatives
- **Same-param-count overloads**: Keep AGBR002 only when multiple overloads share the same parameter count (cannot be disambiguated in JS)

## Non-goals

- **True generic method support**: Method-level type parameters (`<T>` on the method) remain unsupported — this is a permanent limitation of the source-generator approach
- **Generic interface resolution at registration time**: Supporting `IRepository<User>` requires runtime generic resolution, which conflicts with the compile-time-only generator architecture

## Capabilities

### New Capabilities
- `bridge-overload-support`: Method overload disambiguation via parameter-count RPC naming, TypeScript overloaded signatures, and JavaScript argument-length dispatch

### Modified Capabilities
- `bridge-v1-boundary-diagnostics`: Add AGBR006 (open generic interface), improve AGBR001 message, relax AGBR002 for distinct-param-count overloads
- `bridge-typescript-generation`: Support TypeScript overloaded function signatures in generated `.d.ts`
- `bridge-contracts`: Expose overloaded methods with unique RPC names

## Impact

- **ModelExtractor**: Second-pass naming for overloaded methods, generic interface detection
- **BridgeHostEmitter**: No structural changes (already uses `RpcMethodName`)
- **BridgeProxyEmitter**: No structural changes (uses interface method signatures directly)
- **TypeScriptEmitter**: Emit overloaded TypeScript signatures
- **BridgeHostEmitter JS stub**: Generate argument-length dispatcher for overloaded methods
- **BridgeDiagnostics**: Add AGBR006, update AGBR001 message
