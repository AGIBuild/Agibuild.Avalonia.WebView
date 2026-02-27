# framework-positioning-foundation Specification

## Purpose
Define Phase 5 foundation contracts for a framework-grade C# + web development model inspired by proven web-first workflows, while preserving flexible standalone WebView control integration in custom architectures.

## Requirements
### Requirement: Architecture SHALL target framework positioning and dual-path adoption
The system SHALL prioritize framework-positioning outcomes over UI-host count growth, and SHALL preserve both framework-first and control-first adoption paths on the same runtime core.

#### Scenario: Phase acceptance is driven by framework outcomes
- **WHEN** release-readiness for this capability is evaluated
- **THEN** acceptance is based on typed IPC safety, capability governance, deterministic diagnostics, automation coverage, and template DX
- **AND** custom host applications can still adopt the standalone WebView control path without shell coupling

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

### Requirement: Developer experience SHALL remain web-first with integration flexibility
The architecture SHALL keep frontend teams in a web-first workflow, minimize required host-side boilerplate, and retain composable control-level integration for teams with existing host architecture.

#### Scenario: Template demonstrates minimal host code with typed bridge/capability usage
- **WHEN** developers create an app from the recommended template path
- **THEN** they can implement core desktop scenarios via TypeScript + typed bridge contracts
- **AND** host-specific glue code remains minimal and policy-governed

### Requirement: Phase 5 status SHALL be represented as completed when exit criteria evidence is satisfied
Roadmap state for Framework Positioning Foundation SHALL remain represented as completed once all declared Phase 5 exit criteria are met with passing automated evidence, and roadmap governance SHALL additionally declare the currently active next phase for continuous delivery planning.

#### Scenario: Completed status aligns with transition-aware evidence snapshot
- **WHEN** latest full validation gates pass, archived closeout evidence exists, and transition metadata declares the active next phase
- **THEN** roadmap Phase 5 status remains completed
- **AND** closeout evidence reflects current counts and coverage baseline with transition scope metadata

### Requirement: Phase 5 closeout SHALL include deterministic evidence source mapping
Roadmap closeout section SHALL provide explicit mapping from claims to evidence sources, and SHALL include transition mapping that links completed-phase claims to active-phase governance entry points.

#### Scenario: Reviewer traces closeout and transition claims to source artifacts
- **WHEN** reviewer inspects roadmap closeout and transition sections
- **THEN** each key claim has a clear source mapping to archived change evidence or validation command outputs
- **AND** transition entry points are traceable to governed CI/evidence artifacts

### Requirement: Phase closeout evidence SHALL be generated from automated pipeline artifacts
Phase closeout evidence SHALL rely on generated pipeline snapshot artifacts rather than manual aggregation.

#### Scenario: Reviewer validates closeout from generated snapshot
- **WHEN** reviewer checks latest phase closeout evidence
- **THEN** evidence fields are sourced from generated snapshot artifact tied to governed CI commands

