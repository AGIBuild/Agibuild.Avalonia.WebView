# bridge-typescript-generation — Design

**ROADMAP**: Phase 1, Deliverable 1.3

## Overview

`TypeScriptEmitter` maps C# types to TypeScript equivalents (e.g. `Task<T>` → `Promise<T>`, `List<T>` → `T[]`, `string?` → `string | undefined`). It generates per-service interfaces plus a combined `All` constant containing the full `.d.ts` with `declare global { interface Window { agWebView: ... } }` augmentation.

## Architecture

```
[JsExport] / [JsImport] interfaces
         │
         ▼
  WebViewBridgeGenerator
         │
         ▼
  TypeScriptEmitter.EmitDeclarations(exportList, importList)
         │
         ▼
  BridgeTypeScriptDeclarations.g.cs (embedded in consumer assembly)
  ├─ static string AppService
  ├─ static string UiController
  └─ static string All
         │
         ▼
  MSBuild .targets (RoslynCodeTaskFactory inline task)
  - Runs after consumer build
  - Reflects consumer assembly for BridgeTypeScriptDeclarations.All
  - Writes bridge.d.ts to output dir
```

## Key Details

- **Type mapping**: Primitive types, `Task`/`ValueTask`, `List<T>`→`T[]`, `Dictionary<K,V>`→`Record<K,V>`, optional params → `?:`.
- **Per-service constants**: One public static string per service (e.g. `AppService`, `UiController`).
- **All field**: Concatenated `.d.ts` with header + all services + Window augmentation.

## Testing

7 Contract Tests in `TypeScriptGenerationTests`:
- BridgeTypeScriptDeclarations class exists
- All field contains complete .d.ts content
- JsExport generates declaration with methods
- JsImport generates declaration
- Return types correctly mapped
- Parameter types correctly mapped
- All includes Window augmentation
