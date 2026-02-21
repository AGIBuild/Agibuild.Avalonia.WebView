# webview-host-capability-bridge Specification

## Purpose
TBD - created by archiving change phase4-host-capability-bridge. Update Purpose after archive.
## Requirements
### Requirement: Host capability bridge is typed and opt-in
The system SHALL provide an opt-in typed host capability bridge with explicit operations for clipboard, file dialog, external open, notification, application menu, system tray, and supported system actions.

#### Scenario: Host capability bridge is disabled by default
- **WHEN** an app does not configure a host capability bridge
- **THEN** existing shell/runtime behavior remains unchanged and no host capability calls are executed

#### Scenario: Typed clipboard operation returns typed result
- **WHEN** host code invokes clipboard read/write through the capability bridge
- **THEN** the operation returns typed success/failure semantics without stringly-typed command routing

#### Scenario: Typed menu operation returns deterministic outcome
- **WHEN** host code invokes menu update operation through the capability bridge
- **THEN** runtime returns deterministic allow/deny/failure outcome with stable metadata

#### Scenario: Typed tray operation returns deterministic outcome
- **WHEN** host code invokes tray state operation through the capability bridge
- **THEN** runtime returns deterministic allow/deny/failure outcome with stable metadata

### Requirement: Capability authorization is explicit and policy-driven
Each host capability request SHALL be evaluated by an authorization policy that returns explicit allow or deny decisions before provider execution.

#### Scenario: Denied capability request does not execute provider
- **WHEN** authorization policy returns Deny for a capability request
- **THEN** the capability provider is not invoked and runtime returns a denied result with reason metadata

#### Scenario: Allowed capability request executes provider
- **WHEN** authorization policy returns Allow for a capability request
- **THEN** runtime executes the corresponding capability provider and returns typed provider output

### Requirement: Capability failures are isolated and deterministic
Provider exceptions SHALL be isolated from unrelated capabilities and surfaced through defined runtime failure channels.

#### Scenario: Clipboard provider failure does not break external-open capability
- **WHEN** clipboard provider throws during execution
- **THEN** runtime reports clipboard failure deterministically and external-open capability remains functional

#### Scenario: Policy handler failure is surfaced as capability failure
- **WHEN** authorization policy throws during capability evaluation
- **THEN** runtime reports a capability failure result through defined error semantics

### Requirement: Capability bridge behavior is contract-testable and integration-testable
The capability bridge SHALL be fully testable in contract tests with MockAdapter and validated by focused integration tests, including system integration capability paths.

#### Scenario: Contract tests validate allow/deny policy matrix
- **WHEN** contract tests execute capability bridge calls with deterministic policy stubs
- **THEN** allow and deny branches for each capability type are validated without platform dependencies

#### Scenario: Integration tests validate representative desktop capability flow
- **WHEN** integration automation runs representative capability operations
- **THEN** typed results and policy enforcement behavior pass deterministically

#### Scenario: Contract tests validate menu/tray capability matrix
- **WHEN** contract tests execute typed menu/tray operations with policy allow and deny branches
- **THEN** provider execution count and diagnostic outcomes are deterministic and machine-checkable

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

### Requirement: Bridge inbound metadata validation SHALL include aggregate budget stage
The host capability bridge SHALL validate inbound system-integration metadata with an explicit aggregate payload budget stage before policy evaluation and dispatch callbacks.

#### Scenario: Aggregate budget deny bypasses policy/provider execution
- **WHEN** inbound metadata exceeds aggregate budget at boundary validation stage
- **THEN** bridge returns deterministic deny diagnostics and does not invoke policy handler or dispatch subscribers

### Requirement: Boundary diagnostics SHALL identify aggregate budget outcome
Inbound bridge diagnostics SHALL include deterministic deny reason metadata that distinguishes aggregate-budget rejection from other envelope constraints.

#### Scenario: Aggregate budget rejection emits stable reason
- **WHEN** two equivalent over-budget inbound payloads are submitted
- **THEN** diagnostics expose the same boundary-stage deny reason and operation metadata across runs

### Requirement: Bridge metadata budget validation SHALL be deterministic and bounded
The host capability bridge SHALL apply configurable aggregate metadata budget validation using deterministic min/max bounded options before policy evaluation and dispatch.

#### Scenario: Over-budget payload is denied with stable reason using effective configured budget
- **WHEN** inbound metadata total length exceeds effective configured aggregate budget
- **THEN** bridge returns deterministic deny reason and skips policy/provider/dispatch execution

#### Scenario: In-range configured budget drives validation branch deterministically
- **WHEN** host configures an in-range aggregate budget value
- **THEN** bridge uses that value consistently for equivalent inbound payload evaluations

### Requirement: Host capability diagnostics expose explicit schema version
`WebViewHostCapabilityDiagnosticEventArgs` SHALL expose a deterministic `DiagnosticSchemaVersion` field, and runtime emission SHALL set it for every diagnostic event.

#### Scenario: Allow/deny/failure diagnostics carry schema version
- **WHEN** host capability diagnostic events are emitted for allow, deny, or failure outcomes
- **THEN** each event includes a non-zero `DiagnosticSchemaVersion` matching the runtime contract constant

