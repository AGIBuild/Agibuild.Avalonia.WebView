# bridge-typescript-generation — Tasks

## Task 1: Create TypeScriptEmitter
**Acceptance**: `TypeScriptEmitter.EmitDeclarations(exportList, importList)` produces .d.ts string; C#→TS type mapping for primitives, Task, List, optional params.

## Task 2: Wire into WebViewBridgeGenerator
**Acceptance**: Generator calls TypeScriptEmitter; emits `BridgeTypeScriptDeclarations.g.cs` with per-service constants and `All` field; embedded in consumer assembly.

## Task 3: Create MSBuild .targets
**Acceptance**: `.targets` uses RoslynCodeTaskFactory inline task; runs after build; reflects for `BridgeTypeScriptDeclarations.All`; writes `bridge.d.ts` to output directory.

## Task 4: Write 7 tests
**Acceptance**: `TypeScriptGenerationTests` — class exists, All content, JsExport/JsImport declarations, return/param type mapping, Window augmentation.
