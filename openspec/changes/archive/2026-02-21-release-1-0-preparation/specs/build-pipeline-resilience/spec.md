## ADDED Requirements

### Requirement: Stable release pipeline SHALL produce correct NuGet metadata
The `CiPublish` pipeline SHALL validate that stable (non-prerelease) packages meet NuGet publication standards before pushing.

#### Scenario: Stable package passes metadata gate
- **WHEN** `nuke CiPublish` runs for a stable version tag
- **THEN** `ValidatePackage` asserts license, projectUrl, and description are present and appropriate for stable release

#### Scenario: Preview metadata in stable package fails gate
- **WHEN** a stable package contains "preview" or "pre-release" in its description
- **THEN** `ValidatePackage` fails with actionable error before push

### Requirement: README freshness SHALL be enforced by governance test
A CI governance test SHALL assert that README.md test counts and coverage percentage are consistent with latest build evidence.

#### Scenario: README governance test passes
- **WHEN** README metrics match actual test output
- **THEN** governance test passes

#### Scenario: README governance test detects drift
- **WHEN** README claims different test counts than actual TRX/Cobertura evidence
- **THEN** governance test fails with expected vs actual comparison
