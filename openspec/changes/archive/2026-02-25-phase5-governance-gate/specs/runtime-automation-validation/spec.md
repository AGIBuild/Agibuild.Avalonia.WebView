## ADDED Requirements

### Requirement: Runtime critical-path and production matrix SHALL remain closeout-consistent
Governance checks SHALL ensure shared shell closeout IDs remain present across runtime critical-path and shell production matrix artifacts.

#### Scenario: Shared closeout ID missing in either artifact fails governance
- **WHEN** a shared closeout scenario/capability ID is absent in runtime critical-path manifest or production matrix
- **THEN** governance validation fails before release-readiness sign-off
