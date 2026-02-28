## Context

Phase 8 M8.4. Bridge V1 blocks method overloads (AGBR002) and generic methods (AGBR001). Overloads are a natural C# pattern that can be supported via parameter-count disambiguation. Generic methods are a permanent limitation of the source-generator approach (TypeScript generics are erased at runtime). Open generic interfaces silently produce invalid generated code and need a diagnostic.

Current emitter architecture: `ModelExtractor` → `BridgeInterfaceModel` → emitters (Host, Proxy, TypeScript). All emitters consume `RpcMethodName` from the model, so the disambiguation logic can be centralized in `ModelExtractor`.

## Goals / Non-Goals

**Goals:**
- Support method overloads via parameter-count disambiguation in RPC naming
- Add AGBR006 diagnostic for open generic interfaces
- Improve AGBR001 message with actionable alternatives
- Generate TypeScript overloaded function signatures
- Generate JavaScript argument-length dispatcher for overloaded methods

**Non-Goals:**
- Generic method support (permanent limitation)
- Generic interface resolution at registration time
- Overloads with same parameter count (keep AGBR002)

## Decisions

### D1: Overload RPC naming — parameter-count suffix

**Decision**: Overloaded methods get unique RPC names based on parameter count (excluding CancellationToken):
- Fewest params: `Service.methodName` (backward compatible)
- Others: `Service.methodName$N` where N = visible parameter count

**Alternatives considered**:
1. Naming suffix (`searchItems` / `searchItemsWithFilter`) — requires user annotation or heuristic naming, fragile
2. Sequential index (`search$0`, `search$1`) — non-deterministic if method order changes
3. Parameter-count for all (`search$1`, `search$2`) — breaks backward compat for non-overloaded APIs

**Rationale**: Parameter count is deterministic, backward-compatible for the common single-method case, and naturally aligns with JavaScript's `arguments.length` dispatch.

### D2: JavaScript dispatcher — arguments.length check

**Decision**: For overloaded methods, generate a single JS function that dispatches based on `arguments.length`:

```javascript
search: function() {
    if (arguments.length >= 2)
        return rpc.invoke('Svc.search$2', { query: arguments[0], limit: arguments[1] });
    if (arguments.length >= 1)
        return rpc.invoke('Svc.search', { query: arguments[0] });
    return rpc.invoke('Svc.search$0');
}
```

**Rationale**: `arguments.length` is more reliable than checking `!== undefined` (which can't distinguish "not passed" from "explicitly passed undefined"). TypeScript overloaded signatures provide compile-time safety.

### D3: TypeScript — native overloaded signatures

**Decision**: Generate TypeScript interface with multiple signatures:

```typescript
interface MyService {
    search(): Promise<Result[]>;
    search(query: string): Promise<Result[]>;
    search(query: string, limit: number): Promise<Result[]>;
}
```

**Rationale**: TypeScript natively supports overloaded function signatures in interfaces. This provides full type safety without any workaround.

### D4: AGBR002 relaxation — only block same-param-count overloads

**Decision**: AGBR002 is only reported when two or more overloads have the same visible parameter count (excluding CancellationToken). Different-param-count overloads are allowed.

### D5: AGBR006 — open generic interface diagnostic

**Decision**: Add `AGBR006` (Error) for interfaces with open type parameters. Message: "Bridge interface '{0}' has open generic type parameters. Use a concrete closed generic type instead (e.g., IRepository<User> instead of IRepository<T>)."

### D6: ModelExtractor two-pass approach

**Decision**: `ExtractMethods` runs a second pass after all methods are extracted:
1. First pass: extract all methods with default RPC names
2. Second pass: group by CamelCaseName, detect overloads, assign unique RPC names

This keeps the change localized to `ModelExtractor` — all emitters already use `RpcMethodName`.

## Risks / Trade-offs

- [Risk] JS `arguments` is not available in arrow functions → Mitigation: Generated JS uses `function()` declarations (already the case)
- [Risk] CancellationToken exclusion from param count could cause same-count collisions → Mitigation: Validate after CT exclusion, report AGBR002 if collision remains
- [Risk] Adding overloads to an existing non-overloaded method changes no RPC names (fewest keeps original) → Low risk: backward compatible by design

## Testing Strategy

- Unit tests via `CSharpGeneratorDriver`: verify generated RPC names for overloaded methods
- Unit tests for AGBR006 on open generic interfaces
- Unit tests for AGBR002 relaxation (different param counts = no error)
- Unit tests for AGBR002 retention (same param counts = error)
- TypeScript emitter tests for overloaded signatures
- JS stub tests for argument-length dispatcher
- Full regression: all 820 existing tests must pass
