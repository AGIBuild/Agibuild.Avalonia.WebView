## ADDED Requirements

### Requirement: Async and global-coupling closure is tracked in API review
API surface review SHALL track remaining sync wrappers and global mutable coupling points related to async-boundary behavior, with explicit closure status per item.

#### Scenario: Release API audit is executed
- **WHEN** API review runs for a release candidate
- **THEN** each boundary-coupling item is marked as closed, accepted risk, or scheduled removal with owner and target milestone

### Requirement: API review maps public boundaries to executable evidence
API surface review MUST include traceability from boundary-sensitive public APIs to executable contract/runtime tests.

#### Scenario: Boundary API has no runtime evidence mapping
- **WHEN** audit checks API-to-test traceability
- **THEN** review fails until at least one runtime evidence path is linked for that API boundary
