## Context

The Bridge V1 source generator (`Agibuild.Fulora.Bridge.Generator`) extracts interface models and emits BridgeHost/BridgeProxy/TypeScript code. The `ModelExtractor` currently accepts all `MethodKind.Ordinary` methods without validating parameter types, return types, or method signatures against V1 scope boundaries. This means unsupported patterns silently produce broken generated code.

Three separate issues need fixing before V2 feature work:

1. **No compile-time validation** — `ModelExtractor.ExtractMethods` blindly processes everything
2. **Broken nested generic TypeScript mapping** — `TypeScriptEmitter.ExtractGenericArgs` uses `inner.Split(',')` which fails on `Dictionary<string, List<int>>`
3. **Stale JS stubs on Remove** — `RuntimeBridgeService.Remove<T>()` only unregisters RPC handlers, leaving `window.agWebView.bridge.<ServiceName>` intact

## Goals / Non-Goals

**Goals:**
- Emit Roslyn diagnostics (errors) for all V1-unsupported patterns so developers get immediate IDE feedback
- Fix nested generic TypeScript mapping to produce correct output for types like `Record<string, number[]>`
- Clean up JS stubs when services are removed to prevent stale references

**Non-Goals:**
- Adding support for any excluded pattern (generics, overloads, ref/out, CancellationToken, IAsyncEnumerable)
- Changing the JS RPC client architecture or wire protocol
- Adding new public API surface

## Decisions

### D1: Diagnostic reporting location — Generator vs ModelExtractor

**Choice**: Report diagnostics in `WebViewBridgeGenerator.ExtractModel` using `GeneratorAttributeSyntaxContext`, not in `ModelExtractor`.

**Rationale**: `ModelExtractor` returns `BridgeInterfaceModel?` and has no access to `SourceProductionContext` for reporting diagnostics. The generator's `RegisterSourceOutput` callback can report via `spc.ReportDiagnostic()`. We add a validation pass in the generator that checks the extracted model and reports errors before emitting code. When errors are found, code emission is skipped for that interface.

**Alternative considered**: Adding diagnostic info to `BridgeInterfaceModel` — rejected because it couples the model to diagnostic infrastructure.

### D2: Diagnostic IDs and severity

**Choice**: All V1 boundary violations are `DiagnosticSeverity.Error` with IDs `AGBR001`–`AGBR005`.

| ID | Pattern | Message |
|----|---------|---------|
| `AGBR001` | Generic method | Bridge method '{0}' has generic type parameters, which are not supported in V1 |
| `AGBR002` | Method overload | Bridge interface '{0}' has overloaded method '{1}', which is not supported in V1 |
| `AGBR003` | ref/out/in parameter | Bridge method '{0}' has {1} parameter '{2}', which is not supported |
| `AGBR004` | CancellationToken parameter | Bridge method '{0}' has CancellationToken parameter, which is not supported in V1. This will be supported in a future version. |
| `AGBR005` | IAsyncEnumerable return | Bridge method '{0}' returns IAsyncEnumerable, which is not supported in V1. This will be supported in a future version. |

**Rationale**: Error severity prevents broken code from compiling. AGBR004/AGBR005 include "future version" hint. Prefix `AGBR` (Agibuild Bridge) is consistent with the existing `AGWV` prefix for experimental APIs.

### D3: Nested generic parsing strategy

**Choice**: Replace `ExtractGenericArgs` with bracket-depth-aware parsing that tracks `<>` nesting depth and only splits on commas at depth 0.

**Rationale**: This handles all practical cases (`Dictionary<string, List<int>>`, `Dictionary<string, Dictionary<int, bool>>`) without needing a full type parser. The recursive `CSharpTypeToTypeScript` call already handles the inner types correctly once they're properly extracted.

### D4: JS stub cleanup mechanism

**Choice**: In `Remove<T>()`, after unregistering RPC handlers, execute `delete window.agWebView.bridge.<ServiceName>` via `_invokeScript`.

**Rationale**: Minimal change, uses existing infrastructure. The `_invokeScript` delegate is already available in `RuntimeBridgeService`. The `delete` operator gracefully handles the case where the property doesn't exist.

## Risks / Trade-offs

- **[Risk] Existing user code may break if it uses unsupported patterns** → Diagnostics are errors, so users will see compile failures. This is intentional — the alternative (silent broken code) is worse. Mitigation: clear error messages with future-version hints for AGBR004/AGBR005.
- **[Risk] JS stub cleanup may fail silently if `InvokeScriptAsync` throws** → Catch and log; cleanup failure should not prevent Remove from completing.
- **[Risk] Bracket-depth parser may miss edge cases** → Covered by test cases for deeply nested generics. The parser only needs to handle `<>` balancing, not full C# syntax.

## Testing Strategy

- **CT (Generator diagnostics)**: Roslyn `CSharpGeneratorDriver` tests that compile test interfaces with each unsupported pattern and assert the expected diagnostic is reported. Verify that no code is emitted for errored interfaces.
- **CT (Nested generics)**: Unit tests for `CSharpTypeToTypeScript` with inputs like `Dictionary<string, List<int>>`, `List<Dictionary<string, bool>>`, `Dictionary<string, Dictionary<int, List<string>>>`.
- **CT (JS stub cleanup)**: Verify `Remove<T>()` calls `_invokeScript` with the correct `delete` script. Verify cleanup runs even when RPC handler removal is via generated unregister.
