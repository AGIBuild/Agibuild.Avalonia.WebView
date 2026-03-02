## Context

The release infrastructure is largely in place:
- `release.yml` builds on macOS, publishes NuGet packages on tag push, and creates GitHub Releases
- `Build.Packaging.cs` has an `NpmPublish` Nuke target that builds and publishes `@agibuild/bridge`
- `packages/bridge/package.json` is configured at version `1.0.0` with correct metadata
- MinVer derives NuGet version from git tags automatically

**Gap**: The GitHub Actions release workflow does not include npm publish. The npm package has never been published to the registry. No `v1.0.0` tag exists.

**Phase alignment**: Phase 9 / M9.7 (1.0.0 stable release).

## Goals / Non-Goals

**Goals:**
- Add npm publish job to `release.yml` with proper authentication and version sync
- Ensure idempotent publish behavior (re-running on same tag does not break)
- Document the release checklist for creating the first stable tag

**Non-Goals:**
- Changing MinVer configuration or NuGet versioning strategy
- Adding a staging npm registry or NuGet test feed
- Automating tag creation (remains manual for release control)

## Decisions

### D1: npm version sync strategy

**Choice**: Extract version from git tag in CI, use `npm version --no-git-tag-version` to set it before publish.

**Alternatives considered**:
- Keep `package.json` at `1.0.0` and manually bump: Error-prone, version drift risk
- Use a separate npm versioning tool: Overkill for a single package

**Rationale**: Git tags are the single source of version truth (MinVer for NuGet). Deriving npm version from the same tag ensures consistency.

### D2: npm publish job placement

**Choice**: Separate `npm-publish` job parallel to `publish` (NuGet) and `github-release`, all depending on `build`.

**Alternatives considered**:
- Sequential after NuGet publish: Slower, no dependency between them
- Inside the build job: Mixes concerns, harder to retry independently

**Rationale**: Independent jobs allow parallel execution and isolated retry on failure.

### D3: Pre-release tag handling for npm

**Choice**: Publish with `--tag preview` for pre-release versions, `--tag latest` for stable.

**Rationale**: npm dist-tags prevent pre-release versions from becoming the default `npm install` target.

### D4: Node.js version

**Choice**: Use `actions/setup-node@v4` with Node.js 22.x LTS.

**Rationale**: LTS ensures stability in CI. The `@agibuild/bridge` package has no runtime Node.js dependency.

## Risks / Trade-offs

- **[Risk] NPM_TOKEN not configured** → Job fails with clear error. Documented in release checklist.
- **[Risk] npm registry outage during release** → NuGet and GitHub Release proceed independently; npm can be retried.
- **[Risk] Version already published** → `npm publish` fails on duplicate; handled by checking exit code and treating 409 as success.
- **[Trade-off] Manual tag creation** → Accepted: release control remains with maintainers.

## Testing Strategy

- **CI validation**: The existing `ci.yml` already validates build correctness on all 3 platforms.
- **Dry-run**: Before creating `v1.0.0`, test the workflow with a `v1.0.1-rc.1` pre-release tag.
- **Post-publish verification**: After first real publish, verify with `npm info @agibuild/bridge` and `dotnet package search Agibuild.Fulora`.

## Migration Plan

1. Add npm publish job to `release.yml`
2. Configure `NPM_TOKEN` secret in GitHub repo settings (environment: `npm`)
3. Test with a pre-release tag (`v1.0.1-rc.1`)
4. Create `v1.0.0` tag to trigger the first stable release

## Open Questions

- Should we add npm provenance (`--provenance`) for supply chain security? (Nice-to-have, can be added later)
