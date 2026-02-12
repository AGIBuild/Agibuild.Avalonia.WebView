# bridge-source-generator

**Goal**: G1 (Type-Safe Bidirectional Bridge)
**ROADMAP**: Phase 1, Deliverable 1.1b + 1.2
**Depends on**: bridge-contracts (1.1a) ✅

## Problem

`RuntimeBridgeService` (1.1a) uses reflection to discover interface methods and `DispatchProxy` for JS→C# proxies. This works but:
1. Incompatible with NativeAOT (reflection-based method discovery, DispatchProxy)
2. Runtime overhead for serialization context creation
3. No compile-time validation of bridge interface constraints

## Proposed Change

Create a Roslyn Incremental Source Generator that produces:

**For `[JsExport]` interfaces:**
1. `*BridgeRegistration` — implements `IBridgeServiceRegistration<T>`, registers RPC handlers with direct method calls (no reflection)
2. JS client stub as a string constant
3. Assembly-level `[BridgeRegistration]` attribute for runtime discovery

**For `[JsImport]` interfaces:**
1. `*BridgeProxy` — concrete class implementing the interface, each method calls `IWebViewRpcService.InvokeAsync` directly
2. Assembly-level `[BridgeProxy]` attribute for runtime discovery

**For all bridge types:**
1. `BridgeJsonContext` — STJ `JsonSerializerContext` covering all parameter/return types (AOT-safe)

**Runtime integration:**
- `RuntimeBridgeService.Expose<T>()` checks for generated `IBridgeServiceRegistration<T>` via assembly attributes; falls back to reflection if not found
- `RuntimeBridgeService.GetProxy<T>()` checks for generated proxy class; falls back to DispatchProxy if not found
- Zero breaking changes — existing code continues to work

## Non-goals

- TypeScript `.d.ts` file generation (deliverable 1.3)
- MockBridge generation (deliverable 1.5)
- Diagnostic analyzers for bridge interface validation (future enhancement)
