## Purpose
Define deterministic JSON-RPC contracts for JS and C# bidirectional invocation.

## Requirements

### Requirement: IWebViewRpcService interface in Core
The Core assembly SHALL define `IWebViewRpcService`:
- `void Handle(string method, Func<JsonElement?, Task<object?>> handler)` — register async C# handler
- `void Handle(string method, Func<JsonElement?, object?> handler)` — register sync C# handler
- `void RemoveHandler(string method)` — unregister handler
- `Task<JsonElement> InvokeAsync(string method, object? args = null)` — call JS handler, raw result
- `Task<T?> InvokeAsync<T>(string method, object? args = null)` — call JS handler, typed result

#### Scenario: IWebViewRpcService is resolvable
- **WHEN** a consumer references `IWebViewRpcService`
- **THEN** it compiles without missing type errors

### Requirement: IWebView includes Rpc property
The `IWebView` interface SHALL define:
- `IWebViewRpcService? Rpc { get; }`

Returns non-null when the WebMessage bridge is enabled.

#### Scenario: Rpc is available after enabling bridge
- **WHEN** `EnableWebMessageBridge()` is called
- **THEN** `Rpc` returns a non-null `IWebViewRpcService`

### Requirement: JSON-RPC 2.0 protocol over WebMessage
RPC messages SHALL use JSON-RPC 2.0 format:
- Request: `{ "jsonrpc": "2.0", "id": "<uuid>", "method": "<name>", "params": <args> }`
- Success: `{ "jsonrpc": "2.0", "id": "<uuid>", "result": <value> }`
- Error: `{ "jsonrpc": "2.0", "id": "<uuid>", "error": { "code": <int>, "message": "<msg>" } }`

Messages SHALL be transported via the existing WebMessage bridge with a reserved `__rpc` envelope type.

The `params` and `result` fields SHALL be serialized using `CamelCase` naming policy and deserialized with `PropertyNameCaseInsensitive = true`. This means:
- C# property `UserName` serializes to `"userName"` in the JSON payload
- Explicit `[JsonPropertyName]` attributes SHALL take priority over the naming policy
- The RPC envelope fields (`jsonrpc`, `id`, `method`, `params`, `result`, `error`, `code`, `message`) are serialized via source-generated `JsonSerializerContext` and are NOT affected by this policy

#### Scenario: JS→C# call round-trip
- **WHEN** JS calls `window.agWebView.rpc.invoke("add", {a:1, b:2})`
- **AND** C# has registered a handler for "add"
- **THEN** the JS Promise resolves with the handler's return value

#### Scenario: C#→JS call round-trip
- **WHEN** C# calls `Rpc.InvokeAsync("getTheme")`
- **AND** JS has registered a handler for "getTheme"
- **THEN** the C# Task completes with the handler's return value

#### Scenario: C#→JS result uses camelCase property names
- **WHEN** a C# handler returns a record `new UserProfile { UserName = "Alice", IsAdmin = true }`
- **AND** the record does NOT have `[JsonPropertyName]` attributes
- **THEN** the JSON payload received by JS contains `{ "userName": "Alice", "isAdmin": true }`

#### Scenario: C#→JS params use camelCase property names
- **WHEN** C# calls `Rpc.InvokeAsync("setProfile", new { UserName = "Bob" })`
- **THEN** the JSON-RPC request params contain `{ "userName": "Bob" }`

#### Scenario: JsonPropertyName attribute takes priority
- **WHEN** a C# record has `[JsonPropertyName("user_name")] string UserName`
- **THEN** the JSON payload uses `"user_name"` (not `"userName"`)

#### Scenario: JS→C# typed result deserialization is case-insensitive
- **WHEN** C# calls `Rpc.InvokeAsync<UserProfile>("getProfile")`
- **AND** JS returns `{ "userName": "Alice", "isAdmin": true }`
- **THEN** the deserialized `UserProfile` has `UserName == "Alice"` and `IsAdmin == true`

### Requirement: Error propagation
The RPC runtime SHALL propagate errors when a handler throws an exception:
- C# exception → JS receives rejected Promise with error message
- JS error → C# receives exception with error message

#### Scenario: C# handler exception propagates to JS
- **WHEN** a C# handler throws `InvalidOperationException("bad input")`
- **THEN** the JS Promise rejects with an error containing "bad input"

#### Scenario: JS handler error propagates to C#
- **WHEN** a JS handler throws `new Error("not found")`
- **THEN** the C# Task faults with an exception containing "not found"

### Requirement: Unhandled method returns error
When no handler is registered for a method, the callee SHALL return a JSON-RPC error with code `-32601` (Method not found).

#### Scenario: Unknown method returns error
- **WHEN** JS calls `rpc.invoke("nonexistent")`
- **AND** no C# handler is registered for "nonexistent"
- **THEN** the Promise rejects with "Method not found"

### Requirement: JS runtime auto-injection
The RPC JS stub SHALL be automatically injected into the WebView when the WebMessage bridge is enabled. The stub SHALL be available at `window.agWebView.rpc`.

#### Scenario: RPC API is available in JS
- **WHEN** WebMessage bridge is enabled
- **THEN** `window.agWebView.rpc.invoke` is a function
- **AND** `window.agWebView.rpc.handle` is a function

### Requirement: WebView control and WebDialog expose Rpc
Both `WebView` control and `WebDialog` SHALL expose the `Rpc` property.

#### Scenario: Consumer uses RPC from WebView control
- **WHEN** a consumer accesses `webView.Rpc`
- **THEN** it returns the `IWebViewRpcService` instance

### Requirement: RPC service SHALL handle $/cancelRequest notifications
The RPC service SHALL process incoming `$/cancelRequest` notifications that have no `id` field. When received, it SHALL cancel the CancellationTokenSource associated with the specified request ID.

#### Scenario: Cancel request cancels active handler
- **WHEN** a JS→C# RPC call is in progress with request ID "req-1"
- **AND** the RPC service receives `{"jsonrpc":"2.0","method":"$/cancelRequest","params":{"id":"req-1"}}`
- **THEN** the CancellationTokenSource for "req-1" is cancelled

#### Scenario: Cancel request for unknown ID is ignored
- **WHEN** the RPC service receives `$/cancelRequest` for a request ID that is not active
- **THEN** the notification is silently ignored

### Requirement: RPC service SHALL support CancellationTokenSource registration
The RPC service SHALL provide a mechanism for handlers to register a CancellationTokenSource against a request ID, and to clean up after the handler completes.

#### Scenario: CTS registration and cleanup
- **WHEN** a handler registers a CTS for request ID "req-1" and completes normally
- **THEN** the CTS is removed from the active set and disposed

### Requirement: RPC service SHALL handle $/enumerator/next requests
The RPC service SHALL process `$/enumerator/next` requests that reference an active enumerator token. It SHALL advance the enumerator and return the next item(s) with a `finished` flag.

#### Scenario: Next request returns item
- **WHEN** an active enumerator has items remaining
- **AND** `$/enumerator/next` is received with the enumerator token
- **THEN** the response contains `{ values: [item], finished: false }`

#### Scenario: Next request returns finished
- **WHEN** the enumerator has no more items
- **AND** `$/enumerator/next` is received
- **THEN** the response contains `{ values: [], finished: true }`
- **AND** the enumerator is disposed

### Requirement: RPC service SHALL handle $/enumerator/abort notifications
The RPC service SHALL process `$/enumerator/abort` notifications to dispose active enumerators early.

#### Scenario: Abort disposes enumerator
- **WHEN** `$/enumerator/abort` is received for an active token
- **THEN** the enumerator is disposed and removed from active tracking
