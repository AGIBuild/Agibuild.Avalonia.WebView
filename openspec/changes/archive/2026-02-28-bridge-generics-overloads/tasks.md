## 1. Diagnostics Updates

- [x] 1.1 Add AGBR006 descriptor to BridgeDiagnostics.cs for open generic interfaces
- [x] 1.2 Update AGBR001 message to suggest concrete method/generic interface alternatives
- [x] 1.3 Update ModelExtractor.ValidateInterface to detect open generic interfaces (AGBR006)
- [x] 1.4 Update ModelExtractor.ValidateInterface to relax AGBR002 — only report when overloads have same visible param count
- [x] 1.5 Update WebViewBridgeGenerator.ReportDiagnostics to handle AGBR006
- [x] 1.6 Update AnalyzerReleases.Unshipped.md with AGBR006

## 2. Overload RPC Naming

- [x] 2.1 Add VisibleParameterCount property to BridgeMethodModel (excludes CancellationToken)
- [x] 2.2 Update ModelExtractor.ExtractMethods with second-pass overload naming: fewest params keeps original name, others get $N suffix
- [x] 2.3 Verify BridgeHostEmitter/BridgeProxyEmitter work without changes (they use RpcMethodName)

## 3. JavaScript Stub Generation

- [x] 3.1 Update BridgeHostEmitter.EmitGetJsStub to generate argument-length dispatcher for overloaded methods (group by CamelCaseName, emit single dispatching function)

## 4. TypeScript Generation

- [x] 4.1 Update TypeScriptEmitter.GenerateTsInterface to emit multiple signatures for overloaded methods

## 5. Tests

- [x] 5.1 Generator diagnostics tests: AGBR006 for open generic interface, AGBR001 improved message, AGBR002 relaxation for different param counts, AGBR002 retention for same param counts
- [x] 5.2 Overload RPC naming tests: verify unique RPC names, fewest-params-keeps-original, CancellationToken exclusion
- [x] 5.3 TypeScript overload tests: verify multiple signatures generated
- [x] 5.4 JS stub dispatcher tests: verify argument-length routing
- [x] 5.5 Full regression: all existing tests pass
