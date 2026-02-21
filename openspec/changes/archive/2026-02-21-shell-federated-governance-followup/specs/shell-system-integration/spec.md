## ADDED Requirements

### Requirement: Inbound metadata envelope SHALL enforce deterministic aggregate payload budget
The system SHALL enforce a deterministic aggregate metadata payload budget for inbound tray/menu interaction events in addition to entry count and key/value bounds.

#### Scenario: Metadata payload within budget is accepted
- **WHEN** inbound system-integration event metadata satisfies entry, key/value, and aggregate budget constraints
- **THEN** runtime continues governed dispatch evaluation and preserves typed payload delivery semantics

#### Scenario: Metadata payload above budget is rejected before dispatch
- **WHEN** inbound system-integration event metadata exceeds aggregate payload budget
- **THEN** runtime returns deterministic deny with stable boundary reason and does not dispatch event to web
