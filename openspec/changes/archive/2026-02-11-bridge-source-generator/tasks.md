# bridge-source-generator — Tasks

## Task 1: Add discovery contracts to Core
**Files**: `IBridgeServiceRegistration.cs`, `BridgeRegistrationAttribute.cs`, `BridgeProxyAttribute.cs`
**Acceptance**: Core compiles; no platform deps; attributes are assembly-level.

## Task 2: Create Generator project with scaffolding
**Files**: New `Agibuild.Fulora.Bridge.Generator` project (netstandard2.0)
**Acceptance**: Project builds; referenced as analyzer by UnitTests project; empty generator runs without error.

## Task 3: Implement BridgeHostEmitter ([JsExport])
**Files**: `BridgeHostEmitter.cs`, `TypeMapper.cs`
**Acceptance**: For a `[JsExport]` interface, generates a `*BridgeRegistration` class that registers RPC handlers with direct method calls.

## Task 4: Implement JsStubEmitter
**Files**: `JsStubEmitter.cs`
**Acceptance**: Generates JS stub string constant for `[JsExport]` interfaces.

## Task 5: Implement BridgeProxyEmitter ([JsImport])
**Files**: `BridgeProxyEmitter.cs`
**Acceptance**: For a `[JsImport]` interface, generates a concrete proxy class implementing the interface.

## Task 6: Wire generator pipeline (IIncrementalGenerator)
**Files**: `WebViewBridgeGenerator.cs`
**Acceptance**: Generator discovers `[JsExport]` and `[JsImport]` interfaces, emits all artifacts, assembly attributes, and shared BridgeJsonContext.

## Task 7: Update RuntimeBridgeService to prefer generated code
**Files**: `RuntimeBridgeService.cs`
**Acceptance**: `Expose<T>` and `GetProxy<T>` check assembly attributes for generated types; fall back to reflection when not found; existing tests pass.

## Task 8: CT for generated code
**Files**: `BridgeGeneratorTests.cs`
**Acceptance**: Tests verify generated source output, end-to-end registration via generated code, and fallback behavior.
