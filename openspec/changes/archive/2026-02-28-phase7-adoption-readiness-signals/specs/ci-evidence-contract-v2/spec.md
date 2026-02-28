## ADDED Requirements

### Requirement: CI evidence v2 SHALL include adoption-readiness summary
CI evidence contract v2 MUST include structured adoption-readiness summary fields with deterministic state, blocking/advisory counts, and source report linkage.

#### Scenario: Adoption summary is present in release evidence
- **WHEN** `CiPublish` emits release evidence v2
- **THEN** payload includes non-empty adoption readiness summary with deterministic status fields

### Requirement: CI evidence v2 SHALL include adoption finding entries when non-passing
When adoption readiness includes non-passing signals, evidence payload MUST include structured finding entries with policy tier and expected-vs-actual details.

#### Scenario: Adoption finding entries are machine-readable
- **WHEN** adoption readiness contains blocking or advisory findings
- **THEN** evidence payload includes structured entries that can be parsed without free-text interpretation
