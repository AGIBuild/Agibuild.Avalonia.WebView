## ADDED Requirements

### Requirement: Compatibility matrix SHALL include deep-link native registration parity coverage
The compatibility matrix MUST document deep-link native registration and activation-ingress support status for Windows, macOS/iOS, Android, and Linux with explicit acceptance criteria references.

#### Scenario: Matrix includes deep-link native registration row
- **WHEN** a contributor inspects the compatibility matrix capability list
- **THEN** deep-link native registration appears as a governed capability with per-platform support status

#### Scenario: Deep-link parity claims are traceable to executable evidence
- **WHEN** deep-link native registration is marked supported for a platform
- **THEN** matrix entry includes deterministic CT and/or IT evidence mapping for that platform
