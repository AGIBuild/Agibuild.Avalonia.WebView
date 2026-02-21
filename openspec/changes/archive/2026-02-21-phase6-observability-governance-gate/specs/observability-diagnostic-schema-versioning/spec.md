## ADDED Requirements

### Requirement: Diagnostic schema version evolution is governance-locked
Diagnostic schema-version expectations SHALL be sourced from a single shared assertion contract, and governance checks MUST verify that CI gate wiring and lane assertions continue to consume this shared source.

#### Scenario: Shared schema expectation source is removed from lane assertions
- **WHEN** unit/integration/governance assertions no longer reference the shared schema expectation source
- **THEN** governance tests fail with a deterministic schema-continuity violation

#### Scenario: CI gate wiring drifts away from strict governance
- **WHEN** build target dependencies no longer include strict OpenSpec validation gate execution
- **THEN** governance tests fail and block pipeline acceptance
