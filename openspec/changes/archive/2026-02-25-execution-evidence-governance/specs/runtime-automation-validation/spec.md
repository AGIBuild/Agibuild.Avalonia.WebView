## ADDED Requirements

### Requirement: Runtime critical-path governance SHALL validate execution evidence
Runtime critical-path governance SHALL validate that scenario evidence is not only declared but also passed in the latest TRX execution artifacts for the active CI context.

#### Scenario: Missing or failed critical-path test evidence fails governance
- **WHEN** a critical-path scenario mapped to test method is absent or not passed in expected TRX files
- **THEN** governance fails with deterministic diagnostics

### Requirement: Runtime critical-path scenarios SHALL declare CI execution context
Runtime critical-path scenarios SHALL support explicit `ciContext` semantics (`Ci` or `CiPublish`) to avoid incorrect execution expectations.

#### Scenario: Package smoke scenario is CiPublish-scoped
- **WHEN** governance evaluates runtime critical-path manifest
- **THEN** `package-consumption-smoke` is marked with `ciContext = CiPublish`
