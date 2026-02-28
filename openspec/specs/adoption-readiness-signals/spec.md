# adoption-readiness-signals Specification

## Purpose
TBD - created by archiving change phase7-adoption-readiness-signals. Update Purpose after archive.
## Requirements
### Requirement: Adoption readiness SHALL emit deterministic KPI evidence
Adoption readiness governance MUST emit deterministic KPI evidence for documentation freshness, template operability, and runtime critical-path confidence.

#### Scenario: Adoption KPI evidence is complete
- **WHEN** adoption readiness target evaluates current CI outputs
- **THEN** report includes deterministic status fields for docs, templates, and runtime confidence dimensions

### Requirement: Adoption readiness findings SHALL be classified as blocking or advisory
Adoption readiness governance MUST classify each finding into policy tier `blocking` or `advisory` with stable category semantics.

#### Scenario: Adoption finding is classified with policy tier
- **WHEN** an adoption signal fails its threshold
- **THEN** emitted finding includes deterministic policy tier and category metadata

### Requirement: Adoption readiness SHALL provide actionable source mapping
Adoption readiness evidence MUST include source artifact mapping and expected-vs-actual diagnostics for all non-passing findings.

#### Scenario: Non-passing adoption signal includes source mapping
- **WHEN** adoption KPI check is non-passing
- **THEN** evidence entry includes source path, expected value, and actual value

