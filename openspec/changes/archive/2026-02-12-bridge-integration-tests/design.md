# bridge-integration-tests — Design

**ROADMAP**: Phase 1, Deliverables 1.7 + 1.1c

## Overview

Integration tests use `MockWebViewAdapter` + `WebViewCore` (no real browser) but exercise the full bridge stack: `RuntimeBridgeService`, RPC dispatch, JS stub injection, proxy invocation. E2E scenario uses a real WebView in the Integration Test App with `InvokeScriptAsync` to simulate JS calls.

## 8 Integration Tests (BridgeIntegrationTests)

| Category | Test | Coverage |
|----------|------|----------|
| Multi-service | Multiple JsExport services exposed simultaneously | AppService + CustomNameService both callable |
| Multi-service | Export and Import proxies coexist | Expose + GetProxy; both work |
| Lifecycle | Bridge survives multiple expose/remove cycles | 3 cycles of Expose → call → Remove |
| Lifecycle | Calling method on removed service returns -32601 | Method not found after Remove |
| Lifecycle | Dispose prevents all operations | Expose/GetProxy throw ObjectDisposedException |
| Errors | Handler exception returns JSON-RPC error with message | IFailingService.WillFail throws; "Boom!" in response |
| Errors | Different exception types reported | WillThrowInvalidOp; "Bad arg" in response |
| Concurrency | Bridge thread-safe for expose operations | 10 parallel Expose; at least one succeeds; call works |

## E2E Scenario (FeatureE2EViewModel)

- `IE2EGreeter` [JsExport]: `Task<string> Greet(string name)` — C# exposes to JS.
- `IE2ENotifier` [JsImport]: Optional; E2E focuses on JsExport.
- `RunBridgeAsync`: Enable bridge, Expose IE2EGreeter, navigate to test page, `InvokeScriptAsync` to call `agWebView.bridge.E2EGreeter.greet({name:"World"})`, assert result contains "Hello, World!", Remove service.
- Result indicator: `ResultBridge` ("PASS"/"FAIL").

## Generator Reference

Integration Tests project references `Agibuild.Avalonia.WebView.Bridge.Generator` as analyzer so `[JsExport]`/`[JsImport]` interfaces get generated registration/proxy code for E2E.
