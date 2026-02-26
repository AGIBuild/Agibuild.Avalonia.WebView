## ADDED Requirements

### Requirement: Release evidence snapshot SHALL use explicit schema version v2
Governed release evidence artifacts in `CiPublish` MUST include an explicit `schemaVersion` field set to `2` and MUST expose deterministic core fields for test summary, coverage summary, and source artifact references.

#### Scenario: CiPublish writes v2 evidence artifact
- **WHEN** `CiPublish` completes governed validation targets
- **THEN** emitted evidence includes `schemaVersion = 2` and all required core sections

#### Scenario: Missing schema version fails governance
- **WHEN** an evidence artifact is missing `schemaVersion` or declares a non-v2 value
- **THEN** governance rejects the artifact with deterministic schema diagnostics

### Requirement: CI evidence v2 SHALL include provenance metadata
Evidence v2 MUST include provenance fields that identify lane context, producer target, and source artifact lineage needed for machine auditing.

#### Scenario: Provenance fields are complete
- **WHEN** governance validates v2 evidence
- **THEN** lane context and source lineage metadata are present and non-empty

#### Scenario: Incomplete provenance fails gate
- **WHEN** any required provenance field is missing or empty
- **THEN** governance fails before release-readiness sign-off

### Requirement: Evidence v2 SHALL NOT require full upstream artifact hashes
Evidence v2 MUST NOT require content-hash fields for every upstream artifact, and SHALL keep provenance requirements limited to release-critical lineage fields.

#### Scenario: Release evidence is valid without full upstream hashes
- **WHEN** `CiPublish` evidence omits non-release-critical upstream artifact hashes
- **THEN** governance validation still passes if required v2 provenance fields are present
