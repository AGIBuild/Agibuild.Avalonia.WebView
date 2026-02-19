## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Multi-window diagnostics correlate lifecycle and profile outcomes
Multi-window runtime diagnostics SHALL correlate lifecycle transitions with resolved session-permission profile identity per window.

#### Scenario: Lifecycle event can be joined with profile identity
- **WHEN** runtime emits lifecycle and profile diagnostics for a managed window
- **THEN** both streams can be correlated using the same stable window identity
