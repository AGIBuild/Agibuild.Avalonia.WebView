## ADDED Requirements

### Requirement: System action whitelist v2 SHALL explicitly define `ShowAbout`
The system SHALL include `ShowAbout` in typed system action whitelist v2 and SHALL evaluate whitelist and policy deterministically before provider execution.

#### Scenario: `ShowAbout` is allowed by whitelist and policy
- **WHEN** a typed system action request uses `ShowAbout` and policy returns allow
- **THEN** runtime executes provider path and returns deterministic `allow` outcome

#### Scenario: `ShowAbout` is denied by policy
- **WHEN** a typed system action request uses `ShowAbout` and whitelist allows but policy denies
- **THEN** runtime returns deterministic `deny` with stable deny metadata and does not execute provider

### Requirement: Tray event payload v2 SHALL be schema-governed
The system SHALL process tray events with payload v2 that contains stable core fields and bounded extension fields.

#### Scenario: Valid tray payload v2 is accepted
- **WHEN** host emits tray event payload containing required v2 core fields and allowed extensions
- **THEN** runtime accepts and routes the event through governed typed flow with deterministic diagnostics

#### Scenario: Invalid tray payload v2 is rejected deterministically
- **WHEN** host emits tray event payload missing required v2 core fields or containing disallowed extension keys
- **THEN** runtime returns deterministic deny/failure metadata and does not dispatch the event to web
