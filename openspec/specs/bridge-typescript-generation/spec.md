# Bridge TypeScript Generation Spec

## Overview
Automatic TypeScript declaration file (.d.ts) generation from [JsExport] and [JsImport] interfaces.

## Requirements

### RT-1: TypeScriptEmitter
- Maps C# types to TypeScript: string, number, boolean, void, arrays, Records, Date→string, Guid→string
- Generates per-service TS interfaces with JSDoc comments
- Generates combined `BridgeTypeScriptDeclarations.All` with Window augmentation

### RT-2: Source Generator integration
- `WebViewBridgeGenerator` emits `BridgeTypeScriptDeclarations.g.cs` alongside existing outputs
- String constants allow runtime access to TS declarations

### RT-3: MSBuild target
- `Agibuild.Avalonia.WebView.Bridge.Generator.targets` auto-writes `bridge.d.ts` after build
- Configurable via `GenerateBridgeTypeScript` (default: true) and `BridgeTypeScriptOutputDir`
- Uses RoslynCodeTaskFactory inline C# task

## Test Coverage
- 7 CTs in `TypeScriptGenerationTests`
