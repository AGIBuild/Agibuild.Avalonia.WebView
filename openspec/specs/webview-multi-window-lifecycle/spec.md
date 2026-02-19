## Purpose
Define cross-platform, contract-driven multi-window lifecycle semantics for shell scenarios, including strategy selection, lifecycle transitions, identity correlation, and bounded teardown behavior.
## Requirements
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

### Requirement: Managed windows have deterministic lifecycle states
The runtime SHALL define deterministic lifecycle states for managed windows: Created, Attached, Ready, Closing, Closed.

#### Scenario: Lifecycle state order is deterministic for successful window open and close
- **WHEN** a managed window is opened and then closed normally
- **THEN** lifecycle state transitions occur in the fixed order Created -> Attached -> Ready -> Closing -> Closed

#### Scenario: Closed state is terminal
- **WHEN** a managed window reaches Closed
- **THEN** no further lifecycle state transitions are emitted for that window identity

### Requirement: Window identity and relationships are stable
Each managed window SHALL have a stable window id, and runtime SHALL track optional parent window id for child-window relationships and deterministic session-permission profile propagation.

#### Scenario: Child window records parent identity
- **WHEN** runtime creates a managed window from a parent window new-window request
- **THEN** the child window lifecycle metadata includes the parent window id

#### Scenario: Window id remains stable across lifecycle events
- **WHEN** lifecycle events are emitted for a managed window
- **THEN** all events for that window carry the same window id

#### Scenario: Child window profile propagation is deterministic
- **WHEN** runtime resolves session-permission profile for a managed child window
- **THEN** profile evaluation uses parent-child identity context and emits deterministic inherited or overridden profile identity

### Requirement: Multi-window teardown is bounded and leak-safe
Managed window close operations SHALL complete in bounded time and release runtime references deterministically.

#### Scenario: Repeated open-close stress does not leak active window references
- **WHEN** a stress scenario repeatedly opens and closes managed windows
- **THEN** runtime active-window tracking returns to zero after each completed close cycle

#### Scenario: Close request with in-flight operations still reaches Closed
- **WHEN** a managed window receives close while in-flight operations exist
- **THEN** runtime completes teardown and emits Closed within bounded completion constraints

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

### Requirement: Multi-window diagnostics correlate lifecycle and profile outcomes
Multi-window runtime diagnostics SHALL correlate lifecycle transitions with resolved session-permission profile identity per window.

#### Scenario: Lifecycle event can be joined with profile identity
- **WHEN** runtime emits lifecycle and profile diagnostics for a managed window
- **THEN** both streams can be correlated using the same stable window identity

