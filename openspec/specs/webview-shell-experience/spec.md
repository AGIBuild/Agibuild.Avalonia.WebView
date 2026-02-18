## Purpose
Define opt-in, UI-agnostic runtime policies that improve common “shell-like” WebView host behaviors (new windows, downloads, permissions) without changing baseline contract semantics when not enabled.
## Requirements
### Requirement: Shell experience is opt-in and non-breaking
The system SHALL provide an opt-in shell policy foundation that improves common host behaviors (new-window, downloads, permissions) and optional host capability bridge integration without changing baseline WebView contract semantics when not enabled.

#### Scenario: Default runtime behavior is unchanged when shell experience is not enabled
- **WHEN** an app uses `Agibuild.Avalonia.WebView` without enabling shell experience
- **THEN** the baseline behaviors defined by existing specs remain unchanged

#### Scenario: Host capability bridge is optional in shell experience
- **WHEN** shell experience is enabled without host capability bridge configuration
- **THEN** shell policy domains continue to work without host capability execution

### Requirement: New window policy strategies are configurable
The shell experience component SHALL provide a configurable policy for `NewWindowRequested` with at least the following strategies:
- navigate in the current view
- delegate to host-provided callback
- open a runtime-managed secondary window
- open in an external browser

#### Scenario: Navigate-in-place strategy handles NewWindowRequested
- **WHEN** the policy is configured to navigate in the current view and a new-window request occurs with a non-null target URI
- **THEN** the current view navigates to that URI in-place (via existing v1 fallback semantics) and no new window is opened

#### Scenario: Delegate strategy routes the decision to host code
- **WHEN** the policy is configured to delegate and a new-window request occurs
- **THEN** the host callback is invoked with the target URI and can mark the request handled

#### Scenario: Managed-window strategy routes request into lifecycle orchestrator
- **WHEN** the policy is configured for managed-window and a new-window request occurs
- **THEN** shell runtime routes the request to the managed-window lifecycle orchestrator with deterministic window identity assignment

#### Scenario: External-browser strategy routes through host capability bridge when configured
- **WHEN** the policy is configured for external-browser and host capability bridge is enabled
- **THEN** shell runtime routes the target URI to typed external-open capability execution with authorization policy enforcement

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

### Requirement: Shell policy execution order is deterministic
The shell experience foundation SHALL define deterministic execution order for policy domains and runtime fallback behavior.

#### Scenario: Policy decision is applied before fallback
- **WHEN** a shell policy handler is configured for an event domain
- **THEN** runtime applies the handler decision first and only uses fallback behavior when the handler leaves the event unhandled/default

#### Scenario: Runtime fallback remains deterministic
- **WHEN** handler output is absent or explicitly defers to baseline behavior
- **THEN** runtime uses the same fallback behavior for equivalent inputs

#### Scenario: New-window strategy resolution is evaluated before lifecycle execution
- **WHEN** a new-window policy is configured to use managed-window strategy
- **THEN** runtime finalizes strategy resolution before executing lifecycle state transitions for the target window

### Requirement: Policy failures are isolated
A failure in one shell policy handler SHALL NOT corrupt unrelated runtime state, and failure handling SHALL be explicit.

#### Scenario: Handler exception does not mutate unrelated domains
- **WHEN** a shell policy handler throws during event processing
- **THEN** the failure is reported through defined runtime error paths and unrelated shell policy domains continue functioning

