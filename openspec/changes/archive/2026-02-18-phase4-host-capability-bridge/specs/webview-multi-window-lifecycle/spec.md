## MODIFIED Requirements

### Requirement: Multi-window strategy decisions are explicit and contract-driven
The runtime SHALL evaluate each new-window request into an explicit multi-window strategy decision with at least: in-place, managed-window, external-browser, and host-delegate.

#### Scenario: In-place strategy keeps navigation in current view
- **WHEN** a new-window request is resolved as in-place
- **THEN** the current WebView handles navigation and no managed secondary window is created

#### Scenario: Managed-window strategy creates a runtime-managed window
- **WHEN** a new-window request is resolved as managed-window
- **THEN** runtime creates a managed secondary window with a new stable window identity

#### Scenario: External-browser strategy delegates to host capability bridge
- **WHEN** a new-window request is resolved as external-browser and host capability bridge is configured
- **THEN** runtime executes typed external-open capability through authorization-guarded bridge path and does not create managed window

#### Scenario: Host-delegate strategy routes execution decision to host
- **WHEN** a new-window request is resolved as host-delegate
- **THEN** runtime invokes host delegate contract and applies its decision deterministically

### Requirement: Multi-window semantics are testable in contract and integration lanes
The system SHALL make multi-window lifecycle behavior testable via MockAdapter contract tests and focused platform integration tests.

#### Scenario: Contract tests validate strategy mapping and lifecycle ordering
- **WHEN** contract tests run with MockAdapter and deterministic dispatcher
- **THEN** strategy decision mapping and lifecycle state order are validated without platform browser dependencies

#### Scenario: Integration tests validate representative managed-window flow
- **WHEN** integration automation runs representative multi-window flow
- **THEN** open, route, close, and teardown assertions pass on supported desktop targets

#### Scenario: Integration tests validate external-open capability routing
- **WHEN** integration automation runs external-browser strategy with host capability bridge enabled
- **THEN** external-open capability execution and authorization outcomes are validated deterministically
