# runtime-automation-validation Specification

## Purpose
Define the RuntimeAutomation lane requirements for validating real runtime/adapter behaviors (async-boundary, lifecycle, environment isolation, and package smoke), with explicit per-platform execution and skip reporting in CI.
## Requirements
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

### Requirement: Runtime automation critical path SHALL include diagnostic export regression scenario
Runtime automation manifest SHALL include a dedicated scenario id for system-integration diagnostic export protocol verification.

#### Scenario: Critical path lists diagnostic export scenario
- **WHEN** governance reads runtime critical path manifest
- **THEN** scenario id for diagnostic export exists and maps to executable integration evidence

### Requirement: Runtime critical-path and production matrix SHALL remain closeout-consistent
Governance checks SHALL ensure shared shell closeout IDs remain present across runtime critical-path and shell production matrix artifacts.

#### Scenario: Shared closeout ID missing in either artifact fails governance
- **WHEN** a shared closeout scenario/capability ID is absent in runtime critical-path manifest or production matrix
- **THEN** governance validation fails before release-readiness sign-off

### Requirement: Runtime critical path SHALL include product-experience closure scenario
Runtime critical-path manifest SHALL include a product-level shell scenario that validates file/menu capability behavior across permission deny/recover transitions.

#### Scenario: Product-experience closure scenario is listed with executable evidence
- **WHEN** governance reads `runtime-critical-path.manifest.json`
- **THEN** scenario id `shell-product-experience-closure` exists and maps to executable RuntimeAutomation evidence

### Requirement: Runtime critical path SHALL include DevTools lifecycle cycle scenario
Runtime critical-path manifest SHALL include a dedicated shell scenario for DevTools lifecycle stability across repeated scope recreation.

#### Scenario: DevTools lifecycle cycle scenario is present
- **WHEN** governance reads runtime critical-path manifest
- **THEN** scenario id `shell-devtools-lifecycle-cycles` exists and maps to executable RuntimeAutomation evidence

### Requirement: Runtime critical-path governance SHALL validate execution evidence
Runtime critical-path governance SHALL validate that scenario evidence is not only declared but also passed in the latest TRX execution artifacts for the active CI context, and SHALL require evidence-contract v2 provenance fields for each governed scenario mapping.

#### Scenario: Missing or failed critical-path test evidence fails governance
- **WHEN** a critical-path scenario mapped to test method is absent, not passed, or missing required v2 provenance metadata in expected TRX-derived evidence
- **THEN** governance fails with deterministic diagnostics

### Requirement: Runtime critical-path scenarios SHALL declare CI execution context
Runtime critical-path scenarios governed by this change SHALL use explicit `ciContext = CiPublish` semantics and SHALL emit context values that are consistent with evidence-contract v2 release-lane metadata.

#### Scenario: Package smoke scenario is CiPublish-scoped
- **WHEN** governance evaluates runtime critical-path manifest
- **THEN** `package-consumption-smoke` is marked with `ciContext = CiPublish` and lane metadata aligns with `CiPublish` evidence artifacts

