## ADDED Requirements

### Requirement: CI and release SHALL execute in one workflow with staged jobs
The CI/CD orchestration SHALL use a single workflow containing a CI validation job and a release promotion job, with explicit job dependency from release to CI.

#### Scenario: Single-workflow staged execution is enforced
- **WHEN** workflow orchestration is evaluated
- **THEN** CI and release are defined as jobs in the same workflow
- **AND** release job execution depends on successful CI job completion

### Requirement: Release job SHALL require manual approval after CI completion
The release promotion job SHALL be bound to a protected deployment environment with required reviewers so that manual approval is mandatory before any publish action.

#### Scenario: Approval is required before release promotion
- **WHEN** CI job completes successfully
- **THEN** release job enters waiting state pending environment approval
- **AND** publish steps are blocked until reviewers approve deployment

### Requirement: CI and release SHALL share one readiness contract
The build orchestration SHALL define a single machine-checkable readiness contract used by both CI validation and release promotion paths. Release SHALL NOT introduce additional quality gates that are absent from CI readiness.

#### Scenario: Shared readiness contract is enforced
- **WHEN** CI and release stage dependencies are evaluated
- **THEN** both stages reference the same readiness invariant set
- **AND** release-specific checks are limited to publish authorization and artifact provenance verification

### Requirement: Repository version baseline SHALL be fixed at 1.5
The repository-level shared version source SHALL define the baseline major and minor as `1.5` for all packable projects participating in release automation.

#### Scenario: Baseline version is centralized
- **WHEN** version-related repository configuration is evaluated
- **THEN** the baseline major/minor is resolved from a single shared source
- **AND** all NuGet and npm package version computations inherit baseline `1.5`

### Requirement: Active versioning implementation SHALL NOT depend on MinVer
Active build, pack, and release orchestration SHALL NOT rely on MinVer package references, MinVer properties, or MinVer-driven tag derivation in non-archived source paths.

#### Scenario: MinVer-free active graph is enforced
- **WHEN** active repository build/version/release files are evaluated
- **THEN** no MinVer package reference or MinVer property is present in active `.csproj`, central props, workflow, or build orchestration files
- **AND** package version authority remains CI-computed from shared baseline plus run metadata

### Requirement: CI artifact version SHALL follow X.Y.Z.<run_number>
CI version computation SHALL produce artifact/package versions in `X.Y.Z.<run_number>` format and SHALL NOT append textual prerelease identifiers such as `ci`.

#### Scenario: CI version format is compliant
- **WHEN** a CI run computes package version for artifacts
- **THEN** the computed version matches numeric four-part format `X.Y.Z.<run_number>`
- **AND** no `-ci`, `.ci`, or equivalent text suffix is present

### Requirement: Release SHALL publish CI-produced artifacts without rebuild
Release promotion SHALL consume immutable artifacts and version manifest produced by CI and SHALL NOT rebuild packages before publishing.

#### Scenario: Release promotes CI artifacts directly
- **WHEN** release stage starts after CI success and manual approval
- **THEN** release downloads CI artifact bundle and provenance manifest
- **AND** release publishes those exact artifacts after manifest/version parity validation

### Requirement: Tag workflow SHALL NOT act as release version authority
Tag automation (including `create-tag.yml`) SHALL NOT be the authority that computes publishable package versions once this governance model is enabled.

#### Scenario: Version authority remains in CI manifest
- **WHEN** a release publish action starts
- **THEN** publish version is sourced from CI-generated manifest and shared baseline logic
- **AND** tag metadata, if present, is treated as traceability metadata rather than version computation input

### Requirement: `create-tag.yml` SHALL be deleted
The standalone `create-tag.yml` workflow SHALL be removed from the repository. Tag creation moves into the release stage of the unified workflow.

#### Scenario: Standalone tag workflow is absent
- **WHEN** workflow files are evaluated
- **THEN** no `create-tag.yml` file exists in `.github/workflows/`

### Requirement: Pack SHALL depend on test completion
The `Pack` build target SHALL depend on all test targets (`Coverage`, `AutomationLaneReport`) completing successfully before packaging can execute.

#### Scenario: Test-before-pack ordering is enforced
- **WHEN** the build target dependency graph is evaluated
- **THEN** `Pack` has transitive dependencies on `Coverage` and `AutomationLaneReport`
- **AND** `Pack` cannot execute until both test targets have completed successfully

### Requirement: UpdateVersion command SHALL manage version baseline
A Nuke `UpdateVersion` target SHALL exist to modify the `VersionPrefix` in `Directory.Build.props`. Without version argument, it auto-increments patch. With `--update-version-to X.Y.Z`, it validates the new version is strictly greater than current before writing.

#### Scenario: Auto-increment patch version
- **WHEN** `UpdateVersion` is invoked without `--update-version-to`
- **THEN** current `VersionPrefix` patch component is incremented by 1
- **AND** updated `VersionPrefix` is written back to `Directory.Build.props`

#### Scenario: Explicit version update with valid version
- **WHEN** `UpdateVersion` is invoked with `--update-version-to 2.0.0` and current version is `1.5.0`
- **THEN** `VersionPrefix` is updated to `2.0.0`

#### Scenario: Explicit version update with invalid version
- **WHEN** `UpdateVersion` is invoked with `--update-version-to 1.4.0` and current version is `1.5.0`
- **THEN** build fails with error indicating new version must be strictly greater than current

### Requirement: Release stage SHALL include documentation deployment
After manual approval, the release stage SHALL deploy documentation as part of the release process, either by calling `docs-deploy.yml` via `workflow_call` or inline steps.

#### Scenario: Documentation deploys in release
- **WHEN** release stage executes after approval
- **THEN** documentation is built and deployed to GitHub Pages

### Requirement: Release stage SHALL create Git tag and GitHub Release
After package publishing, the release stage SHALL create a Git tag (`vX.Y.Z.<run_number>`) and a GitHub Release associated with that tag.

#### Scenario: Tag and release are created
- **WHEN** package publishing completes successfully in the release stage
- **THEN** a Git tag `v{package_version}` is created targeting the CI commit SHA
- **AND** a GitHub Release is created with auto-generated release notes

### Requirement: Release environment SHALL have required reviewers
The `release` GitHub environment MUST have non-empty `protection_rules` containing `required_reviewers`. An environment binding without protection rules is insufficient.

#### Scenario: Environment protection is configured
- **WHEN** the `release` environment configuration is queried via GitHub API
- **THEN** `protection_rules` contains at least one entry with non-empty `reviewers`
