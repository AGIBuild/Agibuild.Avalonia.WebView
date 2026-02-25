## ADDED Requirements

### Requirement: 1.0 API freeze inventory is complete and versioned
API surface review for the 1.0 release SHALL produce a complete inventory of all public types and members, stored in a canonical, diff-reviewable location, with explicit freeze/deprecation status per item.

#### Scenario: 1.0 freeze inventory is generated
- **WHEN** pre-release API review runs for the 1.0 release train
- **THEN** `docs/API_SURFACE_REVIEW.md` is updated with a timestamped 1.0 freeze inventory listing all public types and their members

#### Scenario: Experimental API is flagged for removal
- **WHEN** a public API introduced during preview is deemed non-essential for 1.0
- **THEN** review marks it `[Obsolete]` with migration guidance or schedules removal for 2.0

### Requirement: Naming convention compliance is enforced for 1.0 surface
All public types, methods, and properties in the 1.0 release SHALL comply with .NET naming guidelines. Non-compliant names introduced during preview SHALL be renamed or marked obsolete.

#### Scenario: Convention violations are listed with remediation
- **WHEN** naming audit runs against the 1.0 public surface
- **THEN** each violation includes the current name, suggested rename, and breaking-change classification
