## ADDED Requirements

### Requirement: Repository test projects SHALL use xUnit v3 package baseline
All repository-owned test projects that currently depend on xUnit v2 packages SHALL migrate to `xunit.v3` while preserving compatibility with `dotnet test` execution in local and CI environments.

#### Scenario: Existing xUnit v2 references are replaced
- **WHEN** test project package references are reviewed after migration
- **THEN** scoped projects no longer reference `xunit` v2 and instead reference `xunit.v3`

#### Scenario: Deterministic test execution remains available
- **WHEN** `dotnet test` is executed for migrated test projects
- **THEN** test discovery and execution succeed without introducing runner-model changes outside the package migration scope
