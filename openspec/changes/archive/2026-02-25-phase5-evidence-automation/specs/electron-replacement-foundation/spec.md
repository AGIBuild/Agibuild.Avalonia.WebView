## ADDED Requirements

### Requirement: Phase closeout evidence SHALL be generated from automated pipeline artifacts
Phase closeout evidence SHALL rely on generated pipeline snapshot artifacts rather than manual aggregation.

#### Scenario: Reviewer validates closeout from generated snapshot
- **WHEN** reviewer checks latest phase closeout evidence
- **THEN** evidence fields are sourced from generated snapshot artifact tied to governed CI commands
