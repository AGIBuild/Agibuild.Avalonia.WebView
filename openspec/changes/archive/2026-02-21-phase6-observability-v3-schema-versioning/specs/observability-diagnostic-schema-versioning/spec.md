## ADDED Requirements

### Requirement: Diagnostic schema assertions are unified across automation lanes
The repository SHALL provide a single shared assertion entrypoint for diagnostic schema invariants that is reusable by ContractAutomation, RuntimeAutomation, and governance tests.

#### Scenario: Unit and integration tests use one schema assertion helper
- **WHEN** CT and IT tests validate diagnostic payload schema invariants
- **THEN** they call the same shared assertion helper instead of duplicating schema checks

#### Scenario: Governance tests validate schema stability through shared assertions
- **WHEN** governance lane validates diagnostic contract stability
- **THEN** it reuses the same schema-version expectation source as CT/IT lanes
