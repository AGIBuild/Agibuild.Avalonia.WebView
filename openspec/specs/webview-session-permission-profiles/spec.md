# webview-session-permission-profiles Specification

## Purpose
TBD - created by archiving change phase4-session-permission-profiles. Update Purpose after archive.
## Requirements
### Requirement: Session-permission profile model is explicit and opt-in
The runtime SHALL define an explicit, opt-in session-permission profile model that binds session scope behavior and permission governance under one profile identity.

#### Scenario: Profile model is not active by default
- **WHEN** a host does not configure session-permission profiles
- **THEN** runtime session and permission behavior remains consistent with existing shell defaults

#### Scenario: Host configures named profile identity
- **WHEN** a host configures a profile for shell-governed runtime
- **THEN** each profile resolution result includes a stable profile identity for diagnostics and audit traces

### Requirement: Profile resolution is deterministic per window context
Profile resolution SHALL be deterministic for equivalent root/parent/window/scope/request context inputs.

#### Scenario: Same context yields same profile decision
- **WHEN** runtime evaluates profile resolution multiple times for equivalent context values
- **THEN** resolved profile identity and decision payload remain identical

#### Scenario: Child window receives context-aware profile resolution
- **WHEN** runtime evaluates profile for a managed child window
- **THEN** the resolver receives parent window identity and can choose inherited or overridden profile deterministically

### Requirement: Permission decisions are profile-driven with explicit fallback
Permission requests SHALL apply profile-defined decisions before fallback behavior, and fallback SHALL remain deterministic when profiles do not define explicit decisions.

#### Scenario: Profile explicitly denies a permission
- **WHEN** a permission request is evaluated and active profile defines Deny for that permission kind
- **THEN** runtime applies Deny deterministically without invoking fallback approval behavior

#### Scenario: Profile has no explicit decision for a permission kind
- **WHEN** a permission request is evaluated and active profile has no explicit decision for that permission kind
- **THEN** runtime uses existing shell permission fallback semantics unchanged

### Requirement: Profile behavior is contract and integration testable
Session-permission profile behavior SHALL be testable via MockAdapter contract tests and focused integration automation.

#### Scenario: Contract tests validate inheritance and override matrix
- **WHEN** contract tests run profile resolution across root and child window contexts
- **THEN** inheritance and override paths are validated without platform browser dependencies

#### Scenario: Integration tests validate profile governance in representative shell flow
- **WHEN** integration automation runs multi-window shell flow with configured profiles
- **THEN** session isolation and permission decisions follow configured profile outcomes deterministically

### Requirement: Session permission profile diagnostics expose explicit schema version
`WebViewSessionPermissionProfileDiagnosticEventArgs` SHALL expose a deterministic `DiagnosticSchemaVersion` field, and runtime emission SHALL set it for every profile diagnostic event.

#### Scenario: Profile diagnostics include schema version across evaluation outcomes
- **WHEN** session/profile diagnostics are emitted for resolved, denied, or fallback permission evaluations
- **THEN** each diagnostic event includes `DiagnosticSchemaVersion` matching the runtime contract constant

