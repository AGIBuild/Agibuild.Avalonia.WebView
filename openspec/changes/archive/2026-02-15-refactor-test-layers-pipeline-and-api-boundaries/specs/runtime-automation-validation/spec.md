## ADDED Requirements

### Requirement: Runtime automation lane is explicitly separated from contract automation
The system SHALL define `RuntimeAutomation` as a distinct lane from mock-based contract tests, with independent execution target(s), result reporting, and pass/fail gating semantics.

#### Scenario: CI publishes lane-specific outcomes
- **WHEN** automation pipelines complete
- **THEN** reports show `ContractAutomation` and `RuntimeAutomation` outcomes separately with independent pass/fail status

### Requirement: Runtime critical-path scenarios are mandatory
The `RuntimeAutomation` lane MUST include critical-path scenarios for async-boundary and lifecycle behavior, including:
- off-thread API invocation marshaling to UI thread
- attach/reattach/dispose lifecycle transitions
- environment-option isolation across instances
- at least one package-consumption smoke path using produced artifacts

#### Scenario: Runtime lane validates off-thread boundary behavior
- **WHEN** runtime automation invokes boundary APIs from non-UI threads
- **THEN** the adapter-facing execution is observed on the UI thread and the scenario fails on mismatch

### Requirement: Runtime lane records platform scope explicitly
Runtime automation output MUST declare executed platform lanes and skipped lanes with reasons.

#### Scenario: Platform lane is skipped
- **WHEN** a platform runtime lane cannot execute in current environment
- **THEN** the report marks it as skipped with an explicit reason instead of silently omitting it
