## Purpose
Define deterministic mock-bridge contracts for unit testing bridge-dependent components.

## Requirements

### Requirement: MockBridgeService implements IBridgeService semantics
The testing harness SHALL provide `MockBridgeService` implementing `IBridgeService` with deterministic behavior for `Expose<T>`, `GetProxy<T>`, and `Remove<T>`.

#### Scenario: Expose and remove operations are observable
- **WHEN** a test exposes and then removes a service type
- **THEN** the mock records the exposure and removes the registration deterministically

### Requirement: MockBridgeService supports proxy setup and assertions
`MockBridgeService` SHALL provide setup and assertion helpers including `SetupProxy<T>`, `WasExposed<T>`, `GetExposedImplementation<T>`, `ExposedCount`, and `Reset()`.

#### Scenario: Proxy setup drives GetProxy deterministically
- **WHEN** a test configures a proxy via `SetupProxy<T>(proxy)`
- **THEN** subsequent `GetProxy<T>()` returns the configured proxy instance

### Requirement: MockBridgeService enforces disposal lifecycle
After `Dispose()` is called, mock bridge operations SHALL fail with `ObjectDisposedException`.

#### Scenario: Operations after dispose are rejected
- **WHEN** a test invokes bridge operations after `Dispose()`
- **THEN** the mock throws `ObjectDisposedException` deterministically
