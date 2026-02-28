## ADDED Requirements

### Requirement: IWebViewRpcService SHALL support CancellationToken-aware handlers
The `IWebViewRpcService` interface SHALL expose methods for registering and retrieving CancellationTokenSources keyed by request ID, enabling cancellation-aware bridge handlers.

#### Scenario: Register and retrieve CTS
- **WHEN** a bridge handler registers a CTS for an active request
- **THEN** the CTS is retrievable by request ID for cancellation
