## ADDED Requirements

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
