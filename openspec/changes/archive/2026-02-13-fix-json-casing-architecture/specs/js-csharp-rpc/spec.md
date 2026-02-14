## MODIFIED Requirements

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
