## Why

Current CI and release orchestration are split across multiple workflows and version authorities. This can cause non-deterministic release behavior and weakens the "CI passed -> Release is safe" guarantee.

This change aligns with `G4` (contract-driven determinism) and roadmap post-phase maintenance priorities after Phase 12, while reinforcing Phase 7/11 release-orchestration outcomes.

## What Changes

- Merge CI and release into one workflow with two stages: CI validation stage and release promotion stage.
- Add a mandatory manual approval gate between CI and release stages using protected environment reviewers.
- Introduce a unified readiness path shared by CI and release stages, with release reusing CI-validated artifacts.
- Freeze the solution version source to major/minor baseline `1.5` (single source of truth at repo level).
- Define CI build version format as `X.Y.Z.<run_number>` without `ci` suffix text.
- Ensure all packable outputs (NuGet and npm package version) are derived from the same computed version in one pipeline execution.
- Remove MinVer package/configuration from active build graph to eliminate dual version authority.
- Remove dependence on tag-time version computation for release publishing; release consumes the CI-produced version manifest and artifacts.
- **BREAKING**: Decommission tag-driven version bumping as release authority (`create-tag.yml` no longer controls package versioning) and fully remove MinVer-based version derivation from active projects.
- Delete `create-tag.yml` workflow entirely; tag creation moves into the release stage of the unified workflow.
- Add `nuke UpdateVersion` command for manual baseline version management (`X.Y.Z` format, auto-increment patch when no version specified, reject if not strictly greater than current).
- Enforce test-before-pack ordering: `Pack` target depends on `Coverage` and `AutomationLaneReport` completing successfully.
- Extend release stage to include documentation deployment (via `workflow_call` to `docs-deploy.yml`), Git tag creation (`vX.Y.Z.<run_number>`), and GitHub Release creation.
- Require `release` environment to have non-empty `required_reviewers` protection rule so approval gate is enforced at the GitHub level.

## Capabilities

### New Capabilities
- `ci-release-version-governance`: Deterministic versioning and artifact-promotion contract where CI computes version and release only promotes verified artifacts.
- `version-baseline-management`: Nuke `UpdateVersion` target for manual version baseline updates with monotonic-increase enforcement.

### Modified Capabilities
- `governance-semantic-assertions`: Extend governance invariants to validate unified readiness, version source consistency, release artifact/version parity, test-before-pack ordering, release environment protection rules, and release side-effect completeness (docs, tag, GitHub Release).

## Impact

- Affected workflows: `.github/workflows/ci.yml` (unified workflow), `.github/workflows/docs-deploy.yml` (converted to `workflow_call`), `.github/workflows/create-tag.yml` (deleted).
- Affected build orchestration: `build/Build.cs`, `build/Build.Packaging.cs` (Pack ordering), `build/Build.Versioning.cs` (new), `build/Build.Governance.cs`, and packaging/version injection points.
- Affected version configuration: repository-level shared version properties (baseline `1.5`) and CI run-number-based patch increment, without MinVer fallback paths.
- Affected governance/testing: update invariants and tests for readiness/version parity, test-before-pack, release environment protection, and release completeness.

## Non-goals

- No partial migration stage; this is a one-step cutover.
- No backward compatibility with old tag-bump release authority.
- No additional release channels beyond the unified CI-computed version strategy in this change.
- No automated version bumping in CI; version baseline updates are developer-initiated via `nuke UpdateVersion`.
