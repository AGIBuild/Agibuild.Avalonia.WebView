## ADDED Requirements

### Requirement: System integration event flow SHALL be typed and policy-governed
The system SHALL provide typed host-to-web system integration events (including tray/menu interaction events) that are routed through policy-governed shell capability flow.

#### Scenario: Allowed tray interaction event is delivered to web
- **WHEN** host emits a typed tray interaction event and policy allows the event flow
- **THEN** runtime delivers the typed event payload to web through the governed typed channel

#### Scenario: Denied tray interaction event is not delivered to web
- **WHEN** host emits a typed tray interaction event and policy denies the event flow
- **THEN** runtime does not deliver the event to web and emits deterministic deny diagnostics

### Requirement: Menu dynamic pruning SHALL be deterministic and policy-derived
The system SHALL compute menu visible/enabled state from policy/context and SHALL produce deterministic results for equivalent inputs.

#### Scenario: Equivalent policy context yields equivalent menu state
- **WHEN** runtime evaluates menu pruning with equivalent policy input context
- **THEN** runtime returns the same effective menu state without duplicate or unstable item transitions

### Requirement: System action whitelist enforcement SHALL be explicit
The system SHALL execute only explicitly allowed typed system actions and SHALL reject non-whitelisted actions with deterministic deny semantics.

#### Scenario: Non-whitelisted action is denied before provider execution
- **WHEN** a typed system action request references an action outside whitelist
- **THEN** runtime returns deterministic deny with reason metadata and provider execution count remains zero
