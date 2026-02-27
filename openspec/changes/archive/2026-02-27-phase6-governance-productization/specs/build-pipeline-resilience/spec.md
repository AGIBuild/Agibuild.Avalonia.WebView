## MODIFIED Requirements

### Requirement: Release pipeline SHALL emit deterministic phase closeout evidence snapshot
The `CiPublish` pipeline SHALL produce a machine-readable closeout snapshot artifact derived from latest test and coverage outputs, and the snapshot MUST conform to CI evidence contract v2 with explicit schema version, provenance metadata, and phase-neutral transition scope metadata.

#### Scenario: CiPublish run writes v2 closeout snapshot JSON
- **WHEN** `CiPublish` pipeline completes governed validation targets
- **THEN** `closeout-snapshot.json` is written with deterministic v2 fields for tests, coverage, lane context, source lineage, and transition scope metadata

#### Scenario: Snapshot schema or transition metadata mismatch fails gate
- **WHEN** snapshot schema version or required v2 transition metadata fields are missing or invalid
- **THEN** governance fails with actionable schema diagnostics before downstream publish actions
