## ADDED Requirements

### Requirement: Shell experience is opt-in and non-breaking
The system SHALL provide an opt-in “shell experience” component that improves common host behaviors without changing baseline WebView contract semantics when not enabled.

#### Scenario: Default runtime behavior is unchanged when shell experience is not enabled
- **WHEN** an app uses `Agibuild.Avalonia.WebView` without enabling shell experience
- **THEN** the baseline behaviors defined by existing specs remain unchanged

### Requirement: New window policy strategies are configurable
The shell experience component SHALL provide a configurable policy for `NewWindowRequested` with at least the following strategies:
- navigate in the current view
- delegate to host-provided callback

#### Scenario: Navigate-in-place strategy handles NewWindowRequested
- **WHEN** the policy is configured to navigate in the current view and a new-window request occurs with a non-null target URI
- **THEN** the current view navigates to that URI in-place (via existing v1 fallback semantics) and no new window is opened

#### Scenario: Delegate strategy routes the decision to host code
- **WHEN** the policy is configured to delegate and a new-window request occurs
- **THEN** the host callback is invoked with the target URI and can mark the request handled

### Requirement: Policy execution is UI-thread consistent and testable
Shell experience policies SHALL be executed on the WebView UI thread and SHALL be testable via MockAdapter without a real browser.

#### Scenario: Policy runs on UI thread
- **WHEN** a new-window request is raised by the runtime
- **THEN** the configured policy executes on the UI thread context

#### Scenario: Policy is testable with MockAdapter
- **WHEN** contract tests run using MockAdapter and a deterministic dispatcher
- **THEN** shell policy behavior can be validated without platform dependencies

### Requirement: Downloads and permissions can be governed by host-defined policy
The shell experience component SHALL allow a host to plug in governance for downloads and permission requests via callbacks/policy interfaces.

#### Scenario: Download governance can cancel or set a path
- **WHEN** a download request is raised and a download policy is configured
- **THEN** the policy can set a download path and/or cancel the request deterministically

#### Scenario: Permission governance can decide allow/deny
- **WHEN** a permission request is raised and a permission policy is configured
- **THEN** the policy can set the permission state to Allow or Deny deterministically
