# Release Automation Pipeline — Spec

## Purpose

Define BDD-style requirements for the automated release pipeline: tag-triggered build, full test suite, NuGet and npm publish, pre-release handling, and release notes. Ensures sustainable multi-package delivery as the Fulora ecosystem grows.

## Requirements

### Requirement: Tag push triggers release workflow

The release workflow SHALL be triggered when a version tag is pushed to the repository.

#### Scenario: v* tag triggers workflow
- **WHEN** a git tag matching `v*` (e.g., `v1.0.0`, `v0.2.1-preview`) is pushed to the repository
- **THEN** the release workflow SHALL execute
- **AND** the workflow SHALL run the build job

#### Scenario: Version extracted from tag
- **GIVEN** a tag `v1.2.3` is pushed
- **WHEN** the workflow runs
- **THEN** the version SHALL be extracted as `1.2.3` (without the `v` prefix)
- **AND** this version SHALL be used for NuGet and npm package versioning

#### Scenario: Manual dispatch supported
- **WHEN** the workflow is manually triggered via `workflow_dispatch`
- **THEN** the user SHALL be able to select or specify a tag
- **AND** the workflow SHALL run with that tag's version

### Requirement: Build job runs full pipeline

The build job SHALL restore, build, test, and pack all packages before publish jobs run.

#### Scenario: Build includes tests
- **WHEN** the build job executes
- **THEN** it SHALL run the full test suite (unit, integration, coverage)
- **AND** the build SHALL fail if tests fail
- **AND** publish jobs SHALL NOT run if the build job fails

#### Scenario: Build produces NuGet artifacts
- **WHEN** the build job completes successfully
- **THEN** it SHALL produce NuGet packages (`.nupkg`) in the artifacts directory
- **AND** these artifacts SHALL be uploaded for the publish job to consume

#### Scenario: MinVer override applied
- **GIVEN** the version extracted from the tag
- **WHEN** NuGet packages are built
- **THEN** the package version SHALL be set via MinVer override (or equivalent) to match the tag
- **AND** all packed packages SHALL have the same version

### Requirement: NuGet publish pushes all packages

The NuGet publish job SHALL push all produced NuGet packages to nuget.org.

#### Scenario: All nupkg files are published
- **GIVEN** the build job produced multiple `.nupkg` files (main library, sub-packages, plugins, CLI, templates)
- **WHEN** the NuGet publish job runs
- **THEN** each package SHALL be pushed to nuget.org
- **AND** the job SHALL use `NUGET_API_KEY` from the `nuget` environment

#### Scenario: Duplicate version is skipped
- **WHEN** a package version already exists on nuget.org
- **THEN** the push SHALL use `--skip-duplicate` (or equivalent)
- **AND** the job SHALL NOT fail due to duplicate version

### Requirement: npm publish pushes all packages in packages/

The npm publish job(s) SHALL publish all npm packages under `packages/` to npmjs.com.

#### Scenario: Bridge package is published
- **GIVEN** `packages/bridge/package.json` exists
- **WHEN** the npm publish job runs
- **THEN** `@agibuild/bridge` SHALL be published to npmjs.com
- **AND** the package version SHALL match the git tag version

#### Scenario: npm publish builds before publishing
- **WHEN** the npm publish job runs for a package
- **THEN** it SHALL run `npm ci` and `npm run build` (or equivalent) before `npm publish`
- **AND** the published package SHALL include built artifacts

#### Scenario: Pre-release tag uses dist tag
- **GIVEN** the git tag is `v1.1.0-preview` (contains `-`)
- **WHEN** npm publish runs
- **THEN** the package SHALL be published with `--tag preview` (or equivalent)
- **AND** `npm install @agibuild/bridge` SHALL NOT install the pre-release by default

#### Scenario: Stable tag uses latest
- **GIVEN** the git tag is `v1.0.0` (no pre-release suffix)
- **WHEN** npm publish runs
- **THEN** the package SHALL be published with `--tag latest`
- **AND** `npm install @agibuild/bridge` SHALL install this version

### Requirement: GitHub Release is created

A GitHub Release SHALL be created with the tag, name, and auto-generated release notes.

#### Scenario: Release created after publish
- **WHEN** both NuGet and npm publish jobs complete successfully
- **THEN** a GitHub Release SHALL be created for the tag
- **AND** the release SHALL use `generate_release_notes: true` (or equivalent) for release notes

#### Scenario: Pre-release flag on GitHub Release
- **GIVEN** the tag contains a pre-release suffix (e.g., `v1.1.0-preview`)
- **WHEN** the GitHub Release is created
- **THEN** the release SHALL be marked as prerelease
- **AND** stable tags SHALL NOT be marked as prerelease

### Requirement: Release workflow uses environments

The publish jobs SHALL use GitHub Environments for secret isolation and optional protection.

#### Scenario: NuGet publish uses nuget environment
- **WHEN** the NuGet publish job runs
- **THEN** it SHALL use the `nuget` environment
- **AND** `NUGET_API_KEY` SHALL be available from that environment's secrets

#### Scenario: npm publish uses npm environment
- **WHEN** the npm publish job runs
- **THEN** it SHALL use the `npm` environment
- **AND** `NPM_TOKEN` SHALL be available from that environment's secrets
