## MODIFIED Requirements

### Requirement: Shell experience is opt-in and non-breaking
The system SHALL provide an opt-in shell policy foundation that improves common host behaviors (new-window, downloads, permissions), supports optional host capability bridge integration, and supports optional system integration governance (menu/tray/system actions) without changing baseline WebView contract semantics when not enabled.

#### Scenario: Default runtime behavior is unchanged when shell experience is not enabled
- **WHEN** an app uses `Agibuild.Avalonia.WebView` without enabling shell experience
- **THEN** the baseline behaviors defined by existing specs remain unchanged

#### Scenario: Host capability bridge is optional in shell experience
- **WHEN** shell experience is enabled without host capability bridge configuration
- **THEN** shell policy domains continue to work without host capability execution

#### Scenario: System integration governance is optional and non-breaking
- **WHEN** shell experience is enabled but system integration policy and provider are not configured
- **THEN** runtime keeps existing non-system-integration behavior unchanged and reports deterministic unavailable outcomes for system integration requests

## ADDED Requirements

### Requirement: System integration operations SHALL execute through shell-governed entry points
The shell experience component SHALL provide deterministic, policy-governed entry points for menu, tray, and supported system actions.

#### Scenario: Policy allows system integration operation
- **WHEN** host invokes shell system integration entry point and policy allows the operation
- **THEN** runtime routes the operation to typed capability bridge provider execution and reports deterministic success

#### Scenario: Policy denies system integration operation
- **WHEN** host invokes shell system integration entry point and policy denies the operation
- **THEN** runtime does not execute provider logic and emits explicit policy failure metadata

### Requirement: System integration policy failures SHALL be isolated
Failure in system integration policy evaluation or provider execution SHALL NOT break other shell policy domains.

#### Scenario: System integration failure does not break permission/download governance
- **WHEN** a system integration operation fails due to policy or provider error
- **THEN** subsequent permission and download policy flows continue to behave deterministically
