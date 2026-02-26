## MODIFIED Requirements

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
