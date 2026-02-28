## MODIFIED Requirements

### Requirement: IBridgeService contract exists in Core assembly
The Core assembly SHALL define an `IBridgeService` interface with:
- `void Expose<T>(T implementation, BridgeOptions? options = null)` — registers a `[JsExport]` service implementation
- `T GetProxy<T>()` — returns a proxy for a `[JsImport]` interface
- `void Remove<T>()` — unregisters a previously exposed service and removes the JS-side stub

#### Scenario: IBridgeService is resolvable
- **WHEN** a consumer references `IBridgeService`
- **THEN** it compiles without missing type errors

#### Scenario: Remove cleans up JS stub
- **WHEN** `Remove<T>()` is called for a previously exposed service
- **THEN** RPC handlers for that service are unregistered
- **AND** `window.agWebView.bridge.<ServiceName>` is deleted via script execution
- **AND** the service name is logged via the bridge tracer

#### Scenario: Remove cleanup tolerates script execution failure
- **WHEN** `Remove<T>()` is called and the JS stub cleanup script fails (e.g., page not loaded)
- **THEN** the RPC handlers are still unregistered
- **AND** the error is logged but does not propagate as an exception
