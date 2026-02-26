## ADDED Requirements

### Requirement: Architecture SHALL target web-first pain-point closure
The system SHALL prioritize solving bundled-browser development pain points over adding additional UI host framework targets.

#### Scenario: Phase acceptance is driven by pain-point outcomes
- **WHEN** release-readiness for this change is evaluated
- **THEN** acceptance is based on typed IPC safety, capability governance, deterministic diagnostics, automation coverage, and template DX
- **AND** WPF/WinForms availability is not required for phase completion

### Requirement: Desktop capability access SHALL be typed and gateway-based
Desktop capability operations SHALL be exposed through a single typed capability gateway instead of scattered host API calls.

#### Scenario: Web app invokes clipboard capability through typed gateway
- **WHEN** frontend or bridge code requests clipboard operations
- **THEN** the runtime routes the request through a typed gateway contract
- **AND** results are returned with stable typed semantics

#### Scenario: Capability calls use consistent outcome model
- **WHEN** any capability request completes
- **THEN** the result maps to deterministic allow/deny/failure semantics with structured metadata

### Requirement: Capability execution SHALL be policy-first
Policy evaluation SHALL happen before provider execution for all host capability calls.

#### Scenario: Denied request never executes provider
- **WHEN** policy evaluation returns deny for a capability request
- **THEN** provider execution is skipped
- **AND** the caller receives a typed denied result with reason metadata

#### Scenario: Policy failure surfaces deterministic failure contract
- **WHEN** policy evaluation throws or fails unexpectedly
- **THEN** runtime returns a deterministic failure result without bypassing policy controls

### Requirement: Runtime diagnostics SHALL be automation- and agent-friendly
The runtime SHALL produce machine-checkable diagnostics for critical flows to support CI governance and AI Agent workflows.

#### Scenario: Critical capability flow produces structured diagnostics
- **WHEN** a request executes the path "web frontend call -> capability gateway -> policy decision -> provider result"
- **THEN** diagnostics include correlation-safe structured events usable by automation

#### Scenario: Critical lifecycle flow is regression-testable
- **WHEN** attach/navigate/capability-call/teardown scenarios run in automation
- **THEN** deterministic assertions can validate behavior without manual log interpretation

### Requirement: Developer experience SHALL remain web-first
The architecture SHALL keep frontend teams in a web-first workflow and minimize required host-side boilerplate.

#### Scenario: Template demonstrates minimal host code with typed bridge/capability usage
- **WHEN** developers create an app from the recommended template path
- **THEN** they can implement core desktop scenarios via TypeScript + typed bridge contracts
- **AND** host-specific glue code remains minimal and policy-governed
