## ADDED Requirements

### Requirement: App-shell demo SHALL provide strategy visualization for system integration
The template app-shell demo SHALL render deterministic strategy output that makes policy and whitelist outcomes machine-checkable.

#### Scenario: Strategy panel shows ShowAbout outcome state
- **WHEN** user triggers ShowAbout action from app-shell demo
- **THEN** panel output includes mode, action, outcome, and deny/failure reason fields

### Requirement: App-shell demo SHALL provide one-click ShowAbout scenario switching
The template SHALL expose deterministic controls for switching between deny-default and explicit allow scenarios without changing source code.

#### Scenario: Scenario switch updates ShowAbout execution branch
- **WHEN** developer toggles the scenario control and re-runs ShowAbout action
- **THEN** result deterministically reflects the selected scenario branch

### Requirement: Template SHALL expose reusable regression check script
The template web bundle SHALL expose a reusable regression check function that runs canonical system-integration demo checks and returns structured results.

#### Scenario: Regression script returns structured checks
- **WHEN** automation invokes the template regression function
- **THEN** function returns machine-readable result entries with stable keys and pass/fail state
