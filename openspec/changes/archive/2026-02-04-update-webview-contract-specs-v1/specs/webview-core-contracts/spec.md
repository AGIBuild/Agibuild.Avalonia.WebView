## MODIFIED Requirements

### Requirement: Event args types
The Core assembly SHALL define event args types:
- `NavigationStartingEventArgs` with `Guid NavigationId { get; }`, `Uri RequestUri { get; }` and `bool Cancel { get; set; }`
- `NavigationCompletedEventArgs` with `Guid NavigationId { get; }`, `Uri RequestUri { get; }`, `NavigationCompletedStatus Status { get; }`, and `Exception? Error { get; }`
- `NewWindowRequestedEventArgs`
- `WebMessageReceivedEventArgs` with `string Body { get; }`, `string Origin { get; }`, and `Guid ChannelId { get; }`
- `WebResourceRequestedEventArgs`
- `EnvironmentRequestedEventArgs`

#### Scenario: Event args are available
- **WHEN** a consumer references the event args types
- **THEN** all listed types are present in the Core assembly

## ADDED Requirements

### Requirement: Navigation and auth status enums
The Core assembly SHALL define the enums:
- `NavigationCompletedStatus` with members: `Success`, `Failure`, `Canceled`, `Superseded`
- `WebAuthStatus` with members: `Success`, `UserCancel`, `Timeout`, `Error`

#### Scenario: Status enums are resolvable
- **WHEN** a consumer references `NavigationCompletedStatus` and `WebAuthStatus`
- **THEN** it compiles without missing type errors

### Requirement: WebMessage drop diagnostics types
The Core assembly SHALL define the enum `WebMessageDropReason` with members:
`OriginNotAllowed`, `ProtocolMismatch`, `ChannelMismatch`.

#### Scenario: Drop reason enum is resolvable
- **WHEN** a consumer references `WebMessageDropReason`
- **THEN** it compiles without missing type errors

### Requirement: Navigation and script exception types
The Core assembly SHALL define exception types:
- `WebViewNavigationException` used for navigation failures
- `WebViewScriptException` used for script execution failures

#### Scenario: Exception types are resolvable
- **WHEN** a consumer references the exception types
- **THEN** it compiles without missing type errors

### Requirement: Core defines IWebViewDispatcher contract
The Core assembly SHALL define an `IWebViewDispatcher` interface to support:
- UI-thread identity checks
- deterministic marshaling of work onto the UI thread for async APIs and event delivery

#### Scenario: IWebViewDispatcher is resolvable
- **WHEN** a consumer references `IWebViewDispatcher` from the Core assembly
- **THEN** it compiles without missing type errors

### Requirement: WebMessage policy contracts are defined in Core
The Core assembly SHALL define WebMessage policy contracts that enable:
- origin allowlisting
- protocol/version checks
- per-instance channel isolation
- deterministic Contract Tests (CT) without relying on platform behavior

#### Scenario: Policy contracts are resolvable
- **WHEN** a consumer references the WebMessage policy contracts
- **THEN** they compile without missing type errors

### Requirement: WebMessage envelope and decision types exist
The Core assembly SHALL define:
- a WebMessage envelope/metadata type exposing `Origin`, `ChannelId`, and `ProtocolVersion`
- a policy decision type exposing allow/deny and, when denied, a `WebMessageDropReason`

#### Scenario: Envelope and decision types are available
- **WHEN** a consumer references the WebMessage envelope and decision types
- **THEN** they compile without missing type errors

### Requirement: Drop diagnostics sink exists
The Core assembly SHALL define a testable diagnostics sink contract for dropped WebMessages.
The diagnostics sink SHALL receive, at minimum: drop reason, origin, and channel id.

#### Scenario: Diagnostics sink contract is resolvable
- **WHEN** a consumer references the diagnostics sink contract
- **THEN** it compiles without missing type errors

