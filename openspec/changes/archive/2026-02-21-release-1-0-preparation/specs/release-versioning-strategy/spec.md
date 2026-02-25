## ADDED Requirements

### Requirement: Version progression follows tag-driven MinVer flow
The release pipeline SHALL derive package versions from git tags using MinVer. A `v1.0.0-preview.N` tag SHALL produce preview packages; a `v1.0.0` tag SHALL produce the stable release.

#### Scenario: Preview tag produces preview packages
- **WHEN** the nearest ancestor tag is `v1.0.0-preview.1`
- **THEN** `nuke Pack` produces packages with version `1.0.0-preview.1+<height>.<commit>`

#### Scenario: Stable tag produces stable packages
- **WHEN** the nearest ancestor tag is `v1.0.0`
- **THEN** `nuke Pack` produces packages with version `1.0.0` (no prerelease suffix)

### Requirement: README metrics SHALL reflect latest test evidence
README.md SHALL contain test counts and coverage percentage that match the latest CI evidence snapshot. A governance test SHALL enforce this invariant.

#### Scenario: README metrics match evidence
- **WHEN** governance tests run against the repository
- **THEN** README.md contains unit test count, integration test count, and line coverage percentage consistent with actual test results

#### Scenario: README metrics drift detected
- **WHEN** actual test counts or coverage differ from README values
- **THEN** governance test fails with actionable message indicating expected vs actual values

### Requirement: CHANGELOG SHALL document 1.0.0 release highlights
The repository SHALL contain a `CHANGELOG.md` following Keep-a-Changelog format that documents the capabilities delivered across Phase 0-5 for the 1.0.0 release.

#### Scenario: CHANGELOG exists and covers 1.0.0
- **WHEN** a contributor or consumer reads the changelog
- **THEN** `CHANGELOG.md` contains a `## [1.0.0]` section with categorized highlights (Added, Changed)

### Requirement: Stable package metadata SHALL meet publication standards
Stable (non-prerelease) NuGet packages SHALL include valid license expression, project URL, and description without "preview" language.

#### Scenario: Stable package nuspec is valid
- **WHEN** `nuke ValidatePackage` runs for a stable-versioned package
- **THEN** nuspec contains non-empty `licenseUrl` or `license`, valid `projectUrl`, and description without "preview" or "pre-release" wording
