## ADDED Requirements

### Requirement: Coverage gate SHALL enforce branch coverage threshold
`Coverage` target SHALL enforce a minimum branch coverage percentage in addition to line coverage.

#### Scenario: Branch coverage below threshold fails gate
- **WHEN** Cobertura `branch-rate` is below configured threshold
- **THEN** `nuke Coverage` fails with actionable threshold diagnostics

#### Scenario: Branch coverage meets threshold
- **WHEN** Cobertura `branch-rate` is at or above configured threshold
- **THEN** coverage gate passes and summary output includes both line and branch metrics

### Requirement: CI SHALL include dependency vulnerability governance gate
`Ci` and `CiPublish` pipelines SHALL depend on a deterministic dependency vulnerability governance target.

#### Scenario: Dependency governance fails in CI graph
- **WHEN** governed dependency scan reports actionable vulnerabilities
- **THEN** `Ci` and `CiPublish` fail before downstream publish/release actions
