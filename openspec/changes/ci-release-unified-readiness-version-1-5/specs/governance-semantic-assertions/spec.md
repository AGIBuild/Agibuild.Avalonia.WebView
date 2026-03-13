## ADDED Requirements

### Requirement: Governance semantic assertions SHALL enforce single-workflow staged topology
Governance checks SHALL assert that CI validation and release promotion are modeled as staged jobs in one workflow definition with explicit release-on-CI dependency.

#### Scenario: Single-workflow topology passes
- **WHEN** workflow graph evidence is evaluated
- **THEN** semantic assertions confirm CI and release jobs exist in one workflow with explicit dependency edge

#### Scenario: Split-workflow topology fails deterministically
- **WHEN** governance detects release orchestration outside the governed unified workflow topology
- **THEN** governance fails with deterministic invariant diagnostics including expected and actual workflow topology

### Requirement: Governance semantic assertions SHALL enforce manual approval gate presence
Governance checks SHALL assert that release promotion job uses protected environment approval metadata before publish actions are allowed.

#### Scenario: Manual approval gate passes
- **WHEN** release stage configuration includes environment with required reviewers
- **THEN** semantic assertions pass for approval-gate invariant

#### Scenario: Missing approval gate fails deterministically
- **WHEN** release stage lacks protected environment approval metadata
- **THEN** governance fails with invariant-keyed diagnostics including missing gate fields

### Requirement: Governance semantic assertions SHALL verify CI-release version provenance parity
Governance checks SHALL assert that release-published version values are identical to the CI-produced manifest version and that both are derived from the same repository baseline source.

#### Scenario: Version provenance parity passes
- **WHEN** CI evidence manifest and release publish inputs are evaluated
- **THEN** semantic assertions confirm version equality across CI manifest and release publish payload
- **AND** diagnostics include baseline source identity used for version derivation

#### Scenario: Version provenance parity fails deterministically
- **WHEN** release publish version differs from CI manifest version or baseline source identity is missing
- **THEN** governance fails with invariant-keyed diagnostics containing expected-vs-actual version and source metadata

### Requirement: Governance semantic assertions SHALL enforce MinVer authority removal
Governance checks SHALL assert that MinVer is not part of active version authority in build/release paths and fail deterministically if MinVer references exist in governed active files.

#### Scenario: MinVer authority removal passes
- **WHEN** governed active build/release/version files are scanned
- **THEN** no MinVer package/config/property reference exists in active authority paths

#### Scenario: MinVer authority removal fails deterministically
- **WHEN** governance detects MinVer reference in active version authority paths
- **THEN** governance fails with invariant-keyed diagnostics including offending file path and detected MinVer token

### Requirement: Governance semantic assertions SHALL enforce no-rebuild promotion policy
Governance checks SHALL assert that release lane does not execute package rebuild steps for promotable artifacts and only consumes CI-produced immutable artifacts.

#### Scenario: No-rebuild promotion policy passes
- **WHEN** release orchestration evidence indicates artifact download and publish actions only
- **THEN** semantic assertions pass for no-rebuild promotion invariant

#### Scenario: Rebuild attempt fails governance
- **WHEN** release lane evidence contains package build/pack execution after CI artifact generation
- **THEN** governance fails deterministically with lane context and violated invariant identifier

### Requirement: Governance diagnostics SHALL expose workflow authority transitions
When tag-driven workflow authority is disabled for version computation, governance diagnostics SHALL include explicit authority metadata to indicate CI manifest authority and tag metadata role.

#### Scenario: Authority metadata is present
- **WHEN** governance evaluates release authority invariants
- **THEN** diagnostic payload includes authority mode, version source, and tag-role classification

### Requirement: Governance semantic assertions SHALL enforce test-before-pack ordering
Governance checks SHALL assert that the `Pack` build target depends on test completion targets (`Coverage`, `AutomationLaneReport`) in the build dependency graph.

#### Scenario: Test-before-pack ordering passes
- **WHEN** build target dependency graph is evaluated
- **THEN** `Pack` has transitive dependencies on `Coverage` and `AutomationLaneReport`

#### Scenario: Pack without test dependency fails governance
- **WHEN** `Pack` target does not depend on test targets
- **THEN** governance fails with invariant-keyed diagnostics indicating missing test dependency

### Requirement: Governance semantic assertions SHALL enforce release stage completeness
Governance checks SHALL assert that the release stage in the unified workflow contains steps for NuGet publish, documentation deployment, tag creation, and GitHub Release creation.

#### Scenario: Release stage completeness passes
- **WHEN** unified workflow release job steps are evaluated
- **THEN** steps exist for package publishing, documentation deployment, tag creation, and GitHub Release creation

### Requirement: Governance semantic assertions SHALL enforce `create-tag.yml` removal
Governance checks SHALL assert that no `create-tag.yml` file exists in `.github/workflows/`.

#### Scenario: `create-tag.yml` removal passes
- **WHEN** workflow directory is evaluated
- **THEN** no `create-tag.yml` file is present

### Requirement: Governance semantic assertions SHALL enforce release environment protection
Governance checks SHALL validate that the `release` environment has `required_reviewers` protection rules configured.

#### Scenario: Release environment protection passes
- **WHEN** release environment configuration is queried
- **THEN** `protection_rules` contain at least one entry with non-empty `reviewers`
