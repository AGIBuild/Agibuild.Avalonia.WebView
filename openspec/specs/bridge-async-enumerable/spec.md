## Purpose
Enable IAsyncEnumerable<T> streaming from C# to JS via AsyncIterator protocol.

## Requirements

### Requirement: Bridge methods SHALL support IAsyncEnumerable return types
A `[JsExport]` interface method MAY return `IAsyncEnumerable<T>`. The source generator SHALL recognize it and generate a streaming handler that implements the pull-based enumerator protocol.

#### Scenario: IAsyncEnumerable method compiles and generates streaming handler
- **WHEN** a `[JsExport]` interface declares `IAsyncEnumerable<string> StreamData(string input)`
- **THEN** the generator emits a BridgeRegistration with a streaming handler
- **AND** the initial RPC response contains a `token` field for the enumerator

### Requirement: Streaming protocol SHALL use pull-based enumerator model
The streaming protocol SHALL implement:
- Initial response: `{ result: { token: "<enum-id>", values?: [item, ...] } }`
- Pull request: `{ method: "$/enumerator/next", params: { token: "<enum-id>" } }` → `{ result: { values: [item, ...], finished: bool } }`
- Abort notification: `{ method: "$/enumerator/abort", params: { token: "<enum-id>" } }`

#### Scenario: Consumer pulls items via enumerator protocol
- **WHEN** JS calls a streaming method and receives a token
- **AND** JS sends `$/enumerator/next` with the token
- **THEN** the C# side returns the next available item(s) and a `finished` flag

#### Scenario: Consumer aborts enumeration early
- **WHEN** JS sends `$/enumerator/abort` for an active token
- **THEN** the C# enumerator is disposed
- **AND** subsequent `$/enumerator/next` for that token returns finished=true

### Requirement: Enumerators SHALL be disposed after completion or timeout
Active enumerators SHALL be disposed when enumeration completes, when abort is received, or after an inactivity timeout of 30 seconds.

#### Scenario: Completed enumerator is cleaned up
- **WHEN** the C# IAsyncEnumerable yields all items
- **AND** the next `$/enumerator/next` is received
- **THEN** the response has `finished: true` and the enumerator is disposed
