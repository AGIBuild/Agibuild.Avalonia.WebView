## ADDED Requirements

### Requirement: CI warning signals are machine-classified
The build pipeline SHALL classify warning outputs into `known-baseline`, `actionable`, and `new-regression` categories and SHALL publish a machine-readable warning report artifact for each CI run.

#### Scenario: New warning appears without baseline classification
- **WHEN** CI encounters a warning that is not present in the approved baseline classification
- **THEN** the warning is classified as `new-regression` and the quality gate fails

### Requirement: WindowsBase conflict warnings are explicitly governed
`WindowsBase` conflict warnings MUST NOT remain unmanaged.
Each accepted baseline conflict SHALL declare owner, rationale, and planned review point in governance metadata.

#### Scenario: Conflict warning lacks governance metadata
- **WHEN** a `WindowsBase` conflict warning exists without owner/rationale metadata
- **THEN** pipeline governance fails with an actionable diagnostic

### Requirement: xUnit analyzer warning policy is bounded and enforceable
xUnit analyzer warnings SHALL be zero for newly added or modified test files unless a scoped suppression with owner and rationale is explicitly declared.
Unscoped or blanket analyzer suppression MUST be rejected by governance checks.

#### Scenario: Modified test introduces unsuppressed analyzer warning
- **WHEN** a touched test file emits an xUnit analyzer warning without approved scoped suppression
- **THEN** the pipeline marks the warning `actionable` and fails the warning governance gate
