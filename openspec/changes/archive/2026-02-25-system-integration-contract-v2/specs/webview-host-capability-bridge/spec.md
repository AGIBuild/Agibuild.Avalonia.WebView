## ADDED Requirements

### Requirement: Host capability bridge SHALL validate tray event payload v2 schema
The host capability bridge SHALL validate inbound tray payload v2 before policy/provider dispatch and SHALL reject invalid payloads deterministically.

#### Scenario: Missing required v2 field blocks dispatch
- **WHEN** bridge receives a tray event payload without required v2 core field
- **THEN** bridge does not dispatch the event and emits deterministic failure metadata

#### Scenario: Disallowed extension key blocks dispatch
- **WHEN** bridge receives a tray event payload with extension key outside allowed namespace
- **THEN** bridge returns deterministic deny/failure and does not raise dispatch event

### Requirement: Bridge diagnostics SHALL expose stable taxonomy for whitelist and payload errors
Bridge diagnostics for system integration v2 SHALL include stable operation and deny/failure classification fields for machine validation.

#### Scenario: Non-whitelisted `ShowAbout` emits stable deny reason
- **WHEN** `ShowAbout` is blocked before provider execution
- **THEN** diagnostics include stable deny reason taxonomy and correlation metadata
