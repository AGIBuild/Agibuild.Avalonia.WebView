# build-pipeline-resilience Specification

## Purpose
Define governance and invariants for the build/packaging pipeline so CI is deterministic across hosts, retries are auditable and bounded, and governed warning classes (e.g., WindowsBase conflicts) are treated as release-blocking regressions.
## Requirements
### Requirement: Package smoke cache root resolution is deterministic
Package smoke and restore-cleanup logic SHALL resolve NuGet global package roots deterministically by honoring explicit environment configuration first, then documented fallback rules.

#### Scenario: Environment-defined package root exists
- **WHEN** `NUGET_PACKAGES` is set for the pipeline
- **THEN** package smoke cleanup and verification use that root and record it in logs

### Requirement: Transient packaging failures are classified before retry
The pipeline MUST classify packaging failures into transient vs deterministic categories before applying retries.  
Retries SHALL be bounded and applied only to failures classified as transient.

#### Scenario: Deterministic failure is encountered
- **WHEN** a deterministic failure category is detected during package smoke
- **THEN** the pipeline fails immediately without retry and preserves diagnostics

### Requirement: Retry behavior is auditable
When retries occur, the pipeline SHALL emit retry count, failure category, and final disposition in machine-readable logs.

#### Scenario: Transient retry succeeds
- **WHEN** a transient category failure succeeds on retry
- **THEN** logs record the original category, retry attempts, and successful final status

### Requirement: WindowsBase conflict warnings are eliminated in governed builds
Governed build targets MUST NOT emit `MSB3277` warnings whose message indicates `WindowsBase` version conflict.
The pipeline SHALL treat this warning pattern as a build-graph defect, not as accepted baseline noise.

#### Scenario: Non-Windows CI build completes without WindowsBase conflict warnings
- **WHEN** warning governance scans outputs from governed build targets on macOS/Linux CI
- **THEN** no `MSB3277` warning containing `WindowsBase` is present in the classification input

#### Scenario: Conflict warning reappears
- **WHEN** a governed build emits `MSB3277` with `WindowsBase` conflict text
- **THEN** warning governance classifies it as `new-regression` and fails the quality gate

### Requirement: Windows dependency boundaries are explicit and host-safe
Project and package references related to the Windows adapter MUST be structured so non-Windows hosts do not resolve Windows-only assembly conflict paths while preserving Windows runtime package correctness.

#### Scenario: Packaging references remain valid for Windows consumers
- **WHEN** the package is restored and used on Windows with the Windows adapter enabled
- **THEN** required WebView2 runtime assemblies remain resolvable without adding manual consumer-side fixes

#### Scenario: Cross-host restore/build remains deterministic
- **WHEN** the same governed targets run on Windows and non-Windows hosts
- **THEN** warning classification output is host-consistent for this conflict class (zero accepted baseline entries)

### Requirement: WebView2 reference model supports host-agnostic pack
The build system MUST allow any supported host OS to build and pack all platform package artifacts without importing WebView2 WPF/WinForms compile references through package targets.

#### Scenario: Package targets are not auto-imported for WebView2 in affected projects
- **WHEN** affected projects evaluate package assets for `Microsoft.Web.WebView2`
- **THEN** `build` and `buildTransitive` target injection is disabled for those projects

#### Scenario: Windows adapter compile still succeeds with explicit core reference
- **WHEN** `Agibuild.Avalonia.WebView.Adapters.Windows` is built on any host
- **THEN** compile-time WebView2 API binding resolves through explicit `Microsoft.Web.WebView2.Core` reference without requiring WPF/WinForms assemblies

### Requirement: CI warning signals are machine-classified
The build pipeline SHALL classify warning outputs into `known-baseline`, `actionable`, and `new-regression` categories and SHALL publish a machine-readable warning report artifact for each CI run.

#### Scenario: New warning appears without baseline classification
- **WHEN** CI encounters a warning that is not present in the approved baseline classification
- **THEN** the warning is classified as `new-regression` and the quality gate fails

### Requirement: WindowsBase conflict warnings are explicitly governed
`WindowsBase` conflict warnings MUST NOT remain unmanaged.
Each accepted baseline conflict SHALL declare owner, rationale, and planned review point in governance metadata.

#### Scenario: Conflict warning lacks governance metadata
- **WHEN** a `WindowsBase` conflict warning exists without owner/rationale metadata
- **THEN** pipeline governance fails with an actionable diagnostic

### Requirement: xUnit analyzer warning policy is bounded and enforceable
xUnit analyzer warnings SHALL be zero for newly added or modified test files unless a scoped suppression with owner and rationale is explicitly declared.
Unscoped or blanket analyzer suppression MUST be rejected by governance checks.

#### Scenario: Modified test introduces unsuppressed analyzer warning
- **WHEN** a touched test file emits an xUnit analyzer warning without approved scoped suppression
- **THEN** the pipeline marks the warning `actionable` and fails the warning governance gate

### Requirement: CI pipelines enforce repository-wide OpenSpec strict validation
Build governance SHALL provide a dedicated target that executes `openspec validate --all --strict`, and `Ci` plus `CiPublish` MUST depend on this target before completion.

#### Scenario: Strict validation fails in CI target graph
- **WHEN** repository specs violate strict OpenSpec rules
- **THEN** the dedicated strict-validation governance target fails and CI/release targets fail deterministically

#### Scenario: Strict validation passes in CI target graph
- **WHEN** all repository specs satisfy strict OpenSpec rules
- **THEN** the governance target succeeds and CI/release pipelines continue to downstream quality gates

### Requirement: CI pipeline SHALL emit deterministic phase closeout evidence snapshot
The build pipeline SHALL produce a machine-readable closeout snapshot artifact derived from latest test and coverage outputs.

#### Scenario: CI run writes snapshot JSON
- **WHEN** CI or release pipeline completes governed validation targets
- **THEN** `phase5-closeout-snapshot.json` is written with deterministic fields for tests, coverage, and source paths

### Requirement: Snapshot generation SHALL fail fast on missing prerequisite artifacts
Snapshot target SHALL fail with clear error when required test or coverage artifacts are unavailable.

#### Scenario: Missing TRX or Cobertura causes explicit failure
- **WHEN** snapshot target runs without required artifact files
- **THEN** target fails with actionable error message indicating missing inputs

### Requirement: Build entry bootstrap SHALL remain deterministic after script modularization
Build script modularization SHALL preserve deterministic Nuke bootstrap behavior and default target dispatch.

#### Scenario: Entry class rename keeps default target execution stable
- **WHEN** build entry class is renamed for clarity
- **THEN** Nuke bootstrap still executes the default build target deterministically
- **AND** target invocation contracts remain unchanged for existing scripts/CI

### Requirement: Build orchestration source SHALL be responsibility-partitioned
Build orchestration implementation SHALL be partitioned into cohesive partial files to improve maintainability without changing target semantics.

#### Scenario: Partial split preserves CI target graph behavior
- **WHEN** build orchestration source is split by responsibilities
- **THEN** `Test`, `Coverage`, `Ci`, and `CiPublish` target behavior and dependencies remain compatible
- **AND** governance checks can still verify critical target and artifact contracts

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

### Requirement: Coverage gate SHALL enforce branch coverage threshold
`Coverage` target SHALL enforce a minimum branch coverage percentage in addition to line coverage.

#### Scenario: Branch coverage below threshold fails gate
- **WHEN** Cobertura `branch-rate` is below configured threshold
- **THEN** `nuke Coverage` fails with actionable threshold diagnostics

#### Scenario: Branch coverage meets threshold
- **WHEN** Cobertura `branch-rate` is at or above configured threshold
- **THEN** coverage gate passes and summary output includes both line and branch metrics

### Requirement: CI SHALL include dependency vulnerability governance gate
`Ci` and `CiPublish` pipelines SHALL depend on a deterministic dependency vulnerability governance target.

#### Scenario: Dependency governance fails in CI graph
- **WHEN** governed dependency scan reports actionable vulnerabilities
- **THEN** `Ci` and `CiPublish` fail before downstream publish/release actions

### Requirement: CI governance SHALL enforce runtime critical-path execution evidence
Build pipeline governance SHALL include a deterministic gate that validates runtime critical-path execution evidence from TRX artifacts.

#### Scenario: CI fails when critical-path execution evidence is incomplete
- **WHEN** CI executes governance targets and required critical-path evidence is missing or failed
- **THEN** pipeline fails with machine-readable failure reasons

