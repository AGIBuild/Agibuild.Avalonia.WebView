## ADDED Requirements

### Requirement: Tray metadata keys SHALL follow reserved-key registry semantics
The system SHALL accept inbound tray metadata keys only when they match reserved keys or the bounded extension lane `platform.extension.*`.

#### Scenario: Reserved key is accepted deterministically
- **WHEN** host emits tray metadata containing a registered key under `platform.*`
- **THEN** runtime accepts the key and continues policy-governed dispatch flow

#### Scenario: Non-reserved key outside extension lane is denied
- **WHEN** host emits tray metadata key under `platform.*` that is not in the reserved registry and not in `platform.extension.*`
- **THEN** runtime returns deterministic deny and does not dispatch the inbound event

### Requirement: Inbound event timestamp SHALL be canonicalized before dispatch
The system SHALL normalize `OccurredAtUtc` to UTC millisecond precision before delivering inbound events to shell/web consumers.

#### Scenario: Sub-millisecond timestamp is normalized
- **WHEN** host emits inbound event with valid UTC timestamp containing sub-millisecond precision
- **THEN** runtime dispatches canonical UTC millisecond timestamp value deterministically
