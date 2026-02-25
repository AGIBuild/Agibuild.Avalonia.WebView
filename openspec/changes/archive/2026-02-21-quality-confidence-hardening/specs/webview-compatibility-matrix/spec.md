## ADDED Requirements

### Requirement: Compatibility matrix SHALL remain synchronized with executable evidence manifests
Compatibility matrix capability entries SHALL map to executable evidence present in runtime automation or contract manifests.

#### Scenario: Matrix entry lacks executable evidence mapping
- **WHEN** governance validation scans matrix capability entries
- **THEN** validation fails if a capability has no linked executable test evidence

#### Scenario: Manifest references capability missing from matrix
- **WHEN** runtime-critical manifest contains a governed capability id
- **THEN** matrix includes the same capability id with platform coverage details

### Requirement: Platform parity claims SHALL be machine-checkable
Platform coverage claims in matrix rows SHALL include deterministic coverage tokens for each declared platform.

#### Scenario: Declared platform has empty coverage token list
- **WHEN** matrix governance checks platform coverage payload
- **THEN** validation fails with capability id and platform name
