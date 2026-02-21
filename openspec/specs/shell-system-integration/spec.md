# shell-system-integration Specification

## Purpose
TBD - created by archiving change shell-system-integration-extension. Update Purpose after archive.
## Requirements
### Requirement: System integration capabilities SHALL be typed and policy-governed
The system SHALL expose menu, tray, and system-action operations through typed capability contracts evaluated by policy before provider execution.

#### Scenario: Allowed system integration request executes provider
- **WHEN** a typed system integration request is submitted and policy returns allow
- **THEN** runtime executes the mapped provider operation and returns deterministic `allow` outcome

#### Scenario: Denied system integration request skips provider
- **WHEN** a typed system integration request is submitted and policy returns deny
- **THEN** runtime does not execute provider logic and returns deterministic `deny` outcome with reason metadata

### Requirement: Menu model updates SHALL be deterministic
The system SHALL support typed menu model updates with deterministic replacement semantics for equivalent inputs.

#### Scenario: Equivalent menu models produce stable runtime state
- **WHEN** host applies the same typed menu model payload repeatedly
- **THEN** runtime state remains stable and does not create duplicate menu entries

### Requirement: Tray state operations SHALL be explicit and auditable
The system SHALL provide typed tray create/update/visibility operations with structured diagnostic events.

#### Scenario: Tray visibility change emits diagnostics
- **WHEN** tray visibility is updated through typed capability call
- **THEN** runtime emits machine-checkable diagnostics containing operation type, outcome, and correlation metadata

### Requirement: System actions SHALL use explicit allowlists
The system SHALL execute system actions only from an explicit typed action allowlist.

#### Scenario: Unknown system action is rejected deterministically
- **WHEN** a system action request references an unsupported action identifier
- **THEN** runtime returns deterministic `failure` or `deny` according to policy contract and does not execute host action

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

### Requirement: Inbound metadata envelope SHALL enforce deterministic aggregate payload budget
The system SHALL enforce a deterministic aggregate metadata payload budget for inbound tray/menu interaction events in addition to entry count and key/value bounds.

#### Scenario: Metadata payload within budget is accepted
- **WHEN** inbound system-integration event metadata satisfies entry, key/value, and aggregate budget constraints
- **THEN** runtime continues governed dispatch evaluation and preserves typed payload delivery semantics

#### Scenario: Metadata payload above budget is rejected before dispatch
- **WHEN** inbound system-integration event metadata exceeds aggregate payload budget
- **THEN** runtime returns deterministic deny with stable boundary reason and does not dispatch event to web

