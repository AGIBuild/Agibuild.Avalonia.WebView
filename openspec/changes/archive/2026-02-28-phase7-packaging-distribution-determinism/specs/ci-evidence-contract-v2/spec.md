## ADDED Requirements

### Requirement: CI evidence v2 SHALL include distribution-readiness summary
CI evidence contract v2 MUST include a structured distribution-readiness summary section containing deterministic status, lane context, and report linkage.

#### Scenario: Distribution summary is present in evidence payload
- **WHEN** release evidence v2 is generated for `CiPublish`
- **THEN** payload contains non-empty distribution readiness section with deterministic state and source artifact reference

### Requirement: CI evidence v2 SHALL expose distribution failure entries as structured data
When distribution readiness fails, evidence payload MUST expose structured failure entries with stable categories and expected-vs-actual details.

#### Scenario: Distribution failure entries are machine-auditable
- **WHEN** distribution readiness state is failed
- **THEN** evidence payload includes structured failure entries that do not require free-text parsing
