## ADDED Requirements

### Requirement: Inbound tray event contracts SHALL expose bounded metadata envelope
The host capability bridge SHALL expose typed inbound tray event contracts with mandatory semantic fields and optional bounded metadata envelope.

#### Scenario: Typed inbound contract validates metadata envelope
- **WHEN** host publishes inbound tray event through typed bridge contract
- **THEN** bridge validates metadata envelope against declared schema bounds before event dispatch

### Requirement: Inbound diagnostics SHALL encode payload boundary decisions
Inbound bridge diagnostics SHALL include machine-checkable fields that identify semantic payload validity and metadata envelope acceptance or rejection.

#### Scenario: Metadata rejection emits deterministic diagnostics
- **WHEN** metadata envelope validation rejects an inbound tray event
- **THEN** diagnostics include correlation identity, boundary decision stage, and deterministic deny/failure category

#### Scenario: Valid metadata envelope keeps unrelated capabilities isolated
- **WHEN** one inbound event passes payload boundary validation and another fails in the same runtime session
- **THEN** unrelated capability operations remain functional and deterministic
