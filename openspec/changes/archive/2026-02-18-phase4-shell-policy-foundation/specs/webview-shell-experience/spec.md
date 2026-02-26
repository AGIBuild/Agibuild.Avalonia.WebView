## MODIFIED Requirements

### Requirement: Shell experience is opt-in and non-breaking
The system SHALL provide an opt-in shell policy foundation that improves common host behaviors (new-window, downloads, permissions) without changing baseline WebView contract semantics when not enabled.

#### Scenario: Default runtime behavior is unchanged when shell experience is not enabled
- **WHEN** an app uses `Agibuild.Fulora` without enabling shell experience
- **THEN** the baseline behaviors defined by existing specs remain unchanged

#### Scenario: Shell policy foundation is composable by hosts
- **WHEN** a host enables shell experience with only a subset of policy domains configured
- **THEN** configured policy domains are applied and unconfigured domains continue using baseline semantics

### Requirement: Policy execution is UI-thread consistent and testable
Shell experience policy handlers SHALL execute on the WebView UI thread, and policy behavior SHALL be testable via MockAdapter without a real browser.

#### Scenario: New-window policy runs on UI thread
- **WHEN** a new-window request is raised by the runtime
- **THEN** the configured new-window policy executes on the UI thread context

#### Scenario: Download policy runs on UI thread
- **WHEN** a download request is raised by the runtime
- **THEN** the configured download policy executes on the UI thread context

#### Scenario: Permission policy runs on UI thread
- **WHEN** a permission request is raised by the runtime
- **THEN** the configured permission policy executes on the UI thread context

#### Scenario: Policy behavior is testable with MockAdapter
- **WHEN** contract tests run using MockAdapter and a deterministic dispatcher
- **THEN** shell policy behavior can be validated without platform dependencies

### Requirement: Downloads and permissions can be governed by host-defined policy
The shell experience component SHALL allow host-defined policy handlers to govern download and permission requests, including explicit fallback semantics when handlers are not configured.

#### Scenario: Download governance can cancel or set a path
- **WHEN** a download request is raised and a download policy is configured
- **THEN** the policy can set a download path and/or cancel the request deterministically

#### Scenario: Download request falls back to baseline behavior when no policy is configured
- **WHEN** a download request is raised and no download policy is configured
- **THEN** runtime keeps baseline download behavior unchanged

#### Scenario: Permission governance can decide allow/deny
- **WHEN** a permission request is raised and a permission policy is configured
- **THEN** the policy can set the permission state to Allow or Deny deterministically

#### Scenario: Permission request falls back to baseline behavior when no policy is configured
- **WHEN** a permission request is raised and no permission policy is configured
- **THEN** runtime keeps baseline permission behavior unchanged

## ADDED Requirements

### Requirement: Shell policy execution order is deterministic
The shell experience foundation SHALL define deterministic execution order for policy domains and runtime fallback behavior.

#### Scenario: Policy decision is applied before fallback
- **WHEN** a shell policy handler is configured for an event domain
- **THEN** runtime applies the handler decision first and only uses fallback behavior when the handler leaves the event unhandled/default

#### Scenario: Runtime fallback remains deterministic
- **WHEN** handler output is absent or explicitly defers to baseline behavior
- **THEN** runtime uses the same fallback behavior for equivalent inputs

### Requirement: Policy failures are isolated
A failure in one shell policy handler SHALL NOT corrupt unrelated runtime state, and failure handling SHALL be explicit.

#### Scenario: Handler exception does not mutate unrelated domains
- **WHEN** a shell policy handler throws during event processing
- **THEN** the failure is reported through defined runtime error paths and unrelated shell policy domains continue functioning
