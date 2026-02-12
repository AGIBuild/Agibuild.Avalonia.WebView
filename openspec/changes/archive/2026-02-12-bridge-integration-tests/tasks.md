# bridge-integration-tests — Tasks

## Task 1: Write 8 integration tests
**Acceptance**: `BridgeIntegrationTests` — multi-service coexistence (2 tests), lifecycle (expose/remove cycles, removed returns -32601, dispose), error boundaries (2 tests), thread safety for expose.

## Task 2: Add Bridge E2E scenario to FeatureE2EViewModel
**Acceptance**: `IE2EGreeter` [JsExport] + `IE2ENotifier` [JsImport]; `RunBridgeAsync` exposes, invokes via script, validates result, removes; `ResultBridge` indicator; scenario runs in "Run All" flow.

## Task 3: Add Generator reference to Integration Tests project
**Acceptance**: Integration Tests project references `Agibuild.Avalonia.WebView.Bridge.Generator` as analyzer (OutputItemType="Analyzer", ReferenceOutputAssembly="false") for generated Bridge types.
