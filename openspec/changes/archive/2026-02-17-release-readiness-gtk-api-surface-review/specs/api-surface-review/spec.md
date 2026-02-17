## ADDED Requirements

### Requirement: API surface review outputs are stored in a canonical, reviewable location
Each API surface review execution SHALL produce a human-reviewable report and SHALL store it at a canonical path within the repository so it can be reviewed by pull request diff.

#### Scenario: Review output is discoverable
- **WHEN** a contributor performs a pre-1.0 API surface review for the release train
- **THEN** the report SHALL be updated in `docs/API_SURFACE_REVIEW.md` (or an explicitly referenced canonical successor file)

### Requirement: API review outputs include traceability pointers to executable evidence
The API surface review report MUST include traceability pointers from boundary-sensitive public APIs to at least one executable evidence path (contract tests and/or runtime automation), or explicitly mark the gap as an actionable item.

#### Scenario: Boundary API is covered by evidence
- **WHEN** the review inspects a boundary-sensitive public API (threading, lifecycle, navigation correlation, WebMessage policy)
- **THEN** the report SHALL link to at least one executable test that validates the boundary behavior OR record an action item to add such evidence

