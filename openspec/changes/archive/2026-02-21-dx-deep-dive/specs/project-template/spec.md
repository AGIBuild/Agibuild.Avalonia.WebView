## ADDED Requirements

### Requirement: Template debug startup path SHALL be deterministic
Hybrid project template SHALL include deterministic web-debug startup conventions for local development.

#### Scenario: Debug startup contract is present in template
- **WHEN** template artifacts are generated
- **THEN** web startup configuration includes deterministic host URL/port handshake and bridge-ready script wiring

### Requirement: Template SHALL preserve bridge typing path
Template-generated web project SHALL compile with generated bridge declaration contracts.

#### Scenario: Template web TypeScript compile succeeds
- **WHEN** template web project runs TypeScript compile validation
- **THEN** bridge type imports resolve and compile without type errors
