## ADDED Requirements

### Requirement: Host capability bridge SHALL support typed inbound system integration events
The host capability bridge SHALL expose typed inbound event contracts for system integration flows so host-originated tray/menu interaction events can be delivered through a governed typed bridge path.

#### Scenario: Inbound event contract is strongly typed
- **WHEN** host emits a system integration event through the bridge contract
- **THEN** the event payload schema is strongly typed and machine-validated by contract tests

### Requirement: Inbound system integration event outcomes SHALL be deterministic
Inbound system integration event handling SHALL produce deterministic allow/deny/failure outcomes with structured diagnostics.

#### Scenario: Inbound event deny includes stable metadata
- **WHEN** policy denies an inbound system integration event
- **THEN** bridge diagnostics include operation identity, deny reason, correlation metadata, and deterministic outcome kind

#### Scenario: Inbound event failure is isolated
- **WHEN** inbound event handling fails for one event type
- **THEN** unrelated outbound/inbound capability operations continue functioning deterministically
