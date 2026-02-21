## ADDED Requirements

### Requirement: ShowAbout system action SHALL be explicit allowlist-governed
The system SHALL treat `ShowAbout` as a typed system action that executes only when explicitly included in configured allowlist policy.

#### Scenario: ShowAbout is denied when not allowlisted
- **WHEN** host issues typed `ShowAbout` system action and allowlist does not include `ShowAbout`
- **THEN** runtime returns deterministic `deny` with reason metadata and provider execution count remains zero

#### Scenario: ShowAbout executes when allowlisted and authorized
- **WHEN** host issues typed `ShowAbout` system action and both allowlist and policy authorize execution
- **THEN** runtime executes provider path and returns deterministic `allow` outcome with stable diagnostics metadata

### Requirement: Tray inbound event payload SHALL use semantic-first bounded envelope
The system SHALL represent tray inbound events with canonical semantic fields and an optional bounded platform metadata envelope under explicit schema constraints.

#### Scenario: Canonical semantic fields are always present
- **WHEN** runtime receives a typed tray inbound event through shell system integration flow
- **THEN** event payload delivered to web includes required canonical semantic fields regardless of platform

#### Scenario: Metadata envelope outside schema constraints is rejected
- **WHEN** tray inbound event includes metadata keys or values outside declared envelope constraints
- **THEN** runtime returns deterministic `deny` or `failure` outcome according to policy contract and does not deliver invalid payload

### Requirement: Menu pruning SHALL support deterministic profile federation
The system SHALL evaluate menu pruning with deterministic federation between session permission profile and shell policy context.

#### Scenario: Profile deny takes precedence before menu mutation
- **WHEN** permission profile denies menu capability in pruning evaluation
- **THEN** runtime does not mutate effective menu state and emits diagnostics identifying profile-derived deny stage

#### Scenario: Equivalent federated inputs produce equivalent pruned state
- **WHEN** pruning runs with equivalent profile + policy input context
- **THEN** runtime returns equivalent effective menu state and stable diagnostics fields across repeated evaluations
