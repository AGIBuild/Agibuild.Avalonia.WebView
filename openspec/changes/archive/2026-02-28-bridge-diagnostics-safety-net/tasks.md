## 1. Generator Diagnostic Infrastructure

- [x] 1.1 Define `DiagnosticDescriptor` constants (AGBR001–AGBR005) in a new `BridgeDiagnostics` static class in the generator project
- [x] 1.2 Add validation method `ValidateBridgeInterface(BridgeInterfaceModel, INamedTypeSymbol)` that checks for all five unsupported patterns and returns a list of diagnostics

## 2. Generator Diagnostic Reporting

- [x] 2.1 Update `WebViewBridgeGenerator` to call validation before emitting code; report diagnostics via `SourceProductionContext.ReportDiagnostic` and skip emission for invalid interfaces
- [x] 2.2 Add CT: generic method → AGBR001 error, no code emitted
- [x] 2.3 Add CT: method overloads → AGBR002 error, no code emitted
- [x] 2.4 Add CT: ref/out/in parameter → AGBR003 error, no code emitted
- [x] 2.5 Add CT: CancellationToken parameter → AGBR004 error with "future version" hint
- [x] 2.6 Add CT: IAsyncEnumerable return → AGBR005 error with "future version" hint
- [x] 2.7 Add CT: one invalid + one valid interface → only valid emits code

## 3. Nested Generic TypeScript Fix

- [x] 3.1 Replace `ExtractGenericArgs` with bracket-depth-aware parsing in `TypeScriptEmitter`
- [x] 3.2 Add CT: `Dictionary<string, List<int>>` → `Record<string, number[]>`
- [x] 3.3 Add CT: `Dictionary<string, Dictionary<int, List<string>>>` → `Record<string, Record<number, string[]>>`
- [x] 3.4 Add CT: `List<Dictionary<string, bool>>` → `Record<string, boolean>[]`

## 4. JS Stub Cleanup on Remove

- [x] 4.1 Update `RuntimeBridgeService.Remove<T>()` to execute `delete window.agWebView.bridge.<ServiceName>` via `_invokeScript`, with error handling
- [x] 4.2 Add CT: Remove calls `_invokeScript` with correct delete script
- [x] 4.3 Add CT: Remove tolerates script execution failure (logs but does not throw)
- [x] 4.4 Verify existing Remove tests still pass

## 5. Validation

- [x] 5.1 Run full test suite (`nuke Test`) and verify all pass
- [x] 5.2 Run coverage check (`nuke Coverage`) and verify threshold met
