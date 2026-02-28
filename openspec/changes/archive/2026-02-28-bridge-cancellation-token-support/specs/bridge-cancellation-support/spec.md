## Purpose
Enable CancellationToken support in bridge interfaces with JS AbortSignal mapping and `$/cancelRequest` protocol extension.

## ADDED Requirements

### Requirement: Bridge methods SHALL accept CancellationToken parameters
A `[JsExport]` interface method MAY include a `System.Threading.CancellationToken` parameter. The source generator SHALL recognize it and wire it to a per-request CancellationTokenSource. The CancellationToken parameter SHALL be excluded from JSON serialization.

#### Scenario: CancellationToken method compiles and generates code
- **WHEN** a `[JsExport]` interface declares `Task<string> Search(string query, CancellationToken ct)`
- **THEN** the generator emits BridgeRegistration with a handler that creates a CTS and passes the token

#### Scenario: CancellationToken parameter is excluded from RPC params
- **WHEN** JS calls `appService.search({ query: "test" })` for a method with CancellationToken
- **THEN** the C# handler receives `query = "test"` and a valid CancellationToken
- **AND** no `ct` field is expected in the JSON params

### Requirement: JS stubs SHALL support AbortSignal for cancellable methods
Generated JS stubs for methods with CancellationToken SHALL accept an optional `options` parameter with `signal?: AbortSignal`. When the signal aborts, the stub SHALL send a `$/cancelRequest` notification.

#### Scenario: JS caller cancels via AbortSignal
- **WHEN** JS calls `appService.search("test", { signal: controller.signal })` and then `controller.abort()`
- **THEN** the stub sends `{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"<request-id>"}}`

### Requirement: Cancelled handler SHALL return error code -32800
When a CancellationToken-aware handler throws `OperationCanceledException`, the RPC layer SHALL return JSON-RPC error with code `-32800` and message "Request cancelled".

#### Scenario: Cancelled operation returns error
- **WHEN** a `$/cancelRequest` is received for an in-progress handler that observes its CancellationToken
- **THEN** the handler throws OperationCanceledException
- **AND** the RPC response contains `error: { code: -32800, message: "Request cancelled" }`
