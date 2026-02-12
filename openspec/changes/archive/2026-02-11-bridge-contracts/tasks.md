# bridge-contracts — Tasks

## Task 1: Add JsExport/JsImport attributes and IBridgeService to Core
**Deliverable**: 1.1a
**Acceptance**: `[JsExport]`, `[JsImport]`, `IBridgeService`, `BridgeOptions` compile in Core assembly; no platform dependencies.
**Files**:
- `src/Agibuild.Avalonia.WebView.Core/JsExportAttribute.cs` — NEW
- `src/Agibuild.Avalonia.WebView.Core/JsImportAttribute.cs` — NEW
- `src/Agibuild.Avalonia.WebView.Core/IBridgeService.cs` — NEW (includes `BridgeOptions`)

## Task 2: Implement RuntimeBridgeService
**Deliverable**: 1.1a
**Acceptance**: `RuntimeBridgeService` implements `IBridgeService`; Expose registers RPC handlers with correct method names; GetProxy creates DispatchProxy; Remove unregisters; JS stub injected on Expose.
**Files**:
- `src/Agibuild.Avalonia.WebView.Runtime/RuntimeBridgeService.cs` — NEW

## Task 3: Wire Bridge into WebViewCore and WebView
**Deliverable**: 1.1a
**Acceptance**: `WebViewCore.Bridge` auto-enables bridge on access; `WebView.Bridge` delegates to core; disposal cleans up bridge.
**Files**:
- `src/Agibuild.Avalonia.WebView.Runtime/WebViewCore.cs` — MODIFIED
- `src/Agibuild.Avalonia.WebView/WebView.cs` — MODIFIED
- `src/Agibuild.Avalonia.WebView.Runtime/WebDialog.cs` — MODIFIED (delegate Bridge)

## Task 4: Contract tests for bridge registration and routing
**Deliverable**: 1.1a
**Acceptance**: All spec scenarios covered by CT. Minimum 20 test cases: attribute validation, handler registration, camelCase naming, custom name, parameter deserialization, return serialization, JS stub injection, auto-enable, duplicate expose, remove, dispose lifecycle, error propagation.
**Files**:
- `tests/Agibuild.Avalonia.WebView.UnitTests/BridgeContractTests.cs` — NEW
- `tests/Agibuild.Avalonia.WebView.Testing/MockWebViewAdapter.cs` — MODIFIED if needed

## Task 5: Contract tests for GetProxy (JsImport)
**Deliverable**: 1.1a
**Acceptance**: GetProxy creates a proxy that routes calls to RPC with correct method names and params; GetProxy on non-JsImport throws; proxy after dispose throws.
**Files**:
- `tests/Agibuild.Avalonia.WebView.UnitTests/BridgeProxyContractTests.cs` — NEW
