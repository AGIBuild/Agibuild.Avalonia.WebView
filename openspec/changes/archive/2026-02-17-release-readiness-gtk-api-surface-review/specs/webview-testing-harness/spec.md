## ADDED Requirements

### Requirement: xUnit v3 package versions are consistent across repository-owned test projects
All repository-owned test projects (including templates and samples) that use xUnit v3 SHALL use a single, consistent package version baseline for the xUnit v3 stack to avoid discovery/execution drift.

#### Scenario: Repository tests use the same xUnit v3 baseline
- **WHEN** package references for `xunit.v3` (and the corresponding test runner packages) are inspected across repo test projects
- **THEN** all referenced versions SHALL match the repository baseline for this release train

### Requirement: Version drift is governed and fails fast
The build or test governance MUST detect and reject xUnit v3 version drift introduced by new or modified test projects.

#### Scenario: A test project introduces a mismatched xUnit version
- **WHEN** a new or modified test project references a different `xunit.v3` version than the baseline
- **THEN** governance SHALL fail with a diagnostic that identifies the project and the mismatched version

