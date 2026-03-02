## Why

Phase 9 (GA Release Readiness) milestones are all marked ✅ Done, yet actual NuGet packages remain at `0.1.21-preview` and `@agibuild/bridge` has never been published to npm. The release pipeline (`release.yml`) lacks npm publish, and no `v1.0.0` tag has been created. This gap prevents external adoption — developers cannot `dotnet add package Agibuild.Fulora.Avalonia` at a stable version or `npm install @agibuild/bridge` from the registry.

**Goal alignment**: Phase 9 / M9.7 (Stable Release Gate), E1 (Project Template readiness). Closes the last mile of the 1.0 GA promise.

## What Changes

- Add `npm-publish` job to `release.yml` so `@agibuild/bridge` is published alongside NuGet packages on tag push
- Add `npm-token` secret requirement documentation for the `npm` environment
- Verify package metadata readiness (license, README, repository URL) for both NuGet and npm
- Document the release checklist: create `v1.0.0` tag → CI builds → NuGet + npm + GitHub Release auto-published

## Non-goals

- Changing the MinVer versioning strategy (already working correctly via git tags)
- Adding a staging/test feed — out of scope for 1.0 GA
- Automated changelog generation — already handled by `softprops/action-gh-release`

## Capabilities

### New Capabilities
- `release-npm-automation`: Automated npm publish step in the release workflow, triggered by version tags

### Modified Capabilities
- `release-distribution-determinism`: Add npm publication as a required distribution channel in the release gate

## Impact

- `.github/workflows/release.yml` — new `npm-publish` job
- `packages/bridge/package.json` — verify metadata completeness
- Documentation: release checklist for creating the first stable tag
- Secrets: `NPM_TOKEN` must be configured in GitHub Actions `npm` environment
