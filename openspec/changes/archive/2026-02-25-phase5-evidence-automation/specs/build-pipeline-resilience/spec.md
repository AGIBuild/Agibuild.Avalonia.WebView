## ADDED Requirements

### Requirement: CI pipeline SHALL emit deterministic phase closeout evidence snapshot
The build pipeline SHALL produce a machine-readable closeout snapshot artifact derived from latest test and coverage outputs.

#### Scenario: CI run writes snapshot JSON
- **WHEN** CI or release pipeline completes governed validation targets
- **THEN** `phase5-closeout-snapshot.json` is written with deterministic fields for tests, coverage, and source paths

### Requirement: Snapshot generation SHALL fail fast on missing prerequisite artifacts
Snapshot target SHALL fail with clear error when required test or coverage artifacts are unavailable.

#### Scenario: Missing TRX or Cobertura causes explicit failure
- **WHEN** snapshot target runs without required artifact files
- **THEN** target fails with actionable error message indicating missing inputs
