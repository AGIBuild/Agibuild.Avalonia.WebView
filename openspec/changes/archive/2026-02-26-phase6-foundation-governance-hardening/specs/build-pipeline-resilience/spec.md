## MODIFIED Requirements

### Requirement: Release pipeline SHALL emit deterministic phase closeout evidence snapshot
The `CiPublish` pipeline SHALL produce a machine-readable closeout snapshot artifact derived from latest test and coverage outputs, and the snapshot MUST conform to CI evidence contract v2 with explicit schema version and provenance metadata.

#### Scenario: CiPublish run writes v2 snapshot JSON
- **WHEN** `CiPublish` pipeline completes governed validation targets
- **THEN** `phase5-closeout-snapshot.json` is written with deterministic v2 fields for tests, coverage, lane context, and source lineage

#### Scenario: Snapshot schema mismatch fails gate
- **WHEN** snapshot schema version or required v2 fields are missing or invalid
- **THEN** governance fails with actionable schema diagnostics before downstream publish actions

### Requirement: Release governance SHALL enforce runtime critical-path execution evidence
Release build governance in `CiPublish` SHALL include a deterministic gate that validates runtime critical-path execution evidence from TRX artifacts and verifies evidence-contract v2 provenance continuity for mapped scenarios.

#### Scenario: CiPublish fails when critical-path execution evidence is incomplete
- **WHEN** `CiPublish` executes governance targets and required critical-path evidence is missing, failed, or lacks required v2 provenance fields
- **THEN** pipeline fails with machine-readable failure reasons
