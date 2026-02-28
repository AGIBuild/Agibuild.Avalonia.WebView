# release-versioning-strategy Specification

## Purpose
Define version progression, release artifacts, and governance for the 1.0 release train (MinVer tag-driven flow, README/CHANGELOG freshness, stable package metadata).
## Requirements
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
Stable release package identities SHALL use the canonical `Agibuild.Fulora.` prefix for primary distributable artifacts.

#### Scenario: Stable package nuspec is valid
- **WHEN** `nuke ValidatePackage` runs for a stable-versioned package
- **THEN** nuspec contains non-empty `licenseUrl` or `license`, valid `projectUrl`, and description without "preview" or "pre-release" wording

#### Scenario: Stable package identity is canonical
- **WHEN** release validation inspects primary stable package outputs
- **THEN** package IDs use the `Agibuild.Fulora.` prefix as canonical identity

### Requirement: Stable publication SHALL require release-orchestration ready state
Tag/version semantics alone MUST NOT authorize stable publication; stable package publication SHALL require release-orchestration decision state `ready`.

#### Scenario: Stable tag with ready decision publishes
- **WHEN** nearest tag indicates stable release and release orchestration decision is `ready`
- **THEN** publication workflow proceeds to package push

#### Scenario: Stable tag with blocked decision is rejected
- **WHEN** nearest tag indicates stable release but release orchestration decision is `blocked`
- **THEN** publication workflow fails before push with deterministic blocking diagnostics

### Requirement: Preview publication SHALL surface orchestration state for auditability
Preview publication flows MUST include release-orchestration decision state in generated evidence even when policy permits publishing with non-blocking advisories.

#### Scenario: Preview release records orchestration state
- **WHEN** preview package workflow runs
- **THEN** release evidence includes orchestration decision state and advisory diagnostics mapping

