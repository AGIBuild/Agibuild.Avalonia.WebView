# Release Automation Pipeline — Design

## Context

Publishing NuGet and npm packages is currently partially automated via GitHub Actions. As the plugin count grows (5+ NuGet packages, 5+ npm packages), a fully automated release pipeline triggered by git tags is essential. The pipeline must build all packages, run the full test suite, publish to nuget.org and npmjs.com, and create GitHub releases with auto-generated notes. Goal: Operational efficiency.

**Existing contracts**: `.github/workflows/release.yml`, Nuke build (`build/Build.cs`, `Build.Packaging.cs`), MinVer versioning, `CiPublish` target, `packages/bridge` npm package, NuGet sub-packages (Core, Runtime, Bridge.Generator, Plugin.LocalStorage, etc.).

## Goals / Non-Goals

### Goals

- GitHub Actions workflow triggered on tag push (`v*`)
- Build all NuGet packages (src/ projects + plugin projects), run full test suite
- Publish all NuGet packages to nuget.org
- Publish all npm packages in `packages/` to npmjs.com
- Version derived from git tag (MinVer override)
- Pre-release tags (e.g., `v1.1.0-preview`) publish as pre-release (NuGet: prerelease, npm: `--tag preview`)
- Release notes auto-generated from conventional commits since last tag

### Non-Goals

- Canary releases
- A/B release testing
- Rollback automation
- Manual approval gates (beyond environment protection)

## Decisions

### D1: Tag-driven trigger

**Decision**: The release workflow SHALL be triggered by `push: tags: ['v*']`. Version is extracted from the tag (e.g., `v1.0.0` → `1.0.0`). `workflow_dispatch` allows manual trigger with tag selection.

**Rationale**: Git tags are the standard release trigger. MinVer is already configured; `PackageVersion` override ensures all packages use the tag version. Manual dispatch supports re-runs or delayed releases.

### D2: Single build job, parallel publish jobs

**Decision**: One `build` job runs the full pipeline (restore, build, test, pack) and uploads NuGet artifacts. Separate `publish` (NuGet) and `npm-publish` jobs consume artifacts or run in package directories. Jobs run in parallel where possible (NuGet and npm can run in parallel after build).

**Rationale**: Build once, publish many. NuGet and npm publish are independent; parallel execution reduces total time. Current workflow structure already follows this pattern.

### D3: NuGet — publish all packed packages

**Decision**: The `Pack` target SHALL produce all NuGet packages (main fat bundle, Core, Runtime, Bridge.Generator, Adapters, Plugins, CLI, Testing, Templates). The `Publish` target (or GitHub Actions publish step) SHALL push all `*.nupkg` (excluding symbols) to nuget.org with `--skip-duplicate`.

**Rationale**: `Build.Packaging.cs` already packs multiple projects. Ensure the list includes all publishable projects. `--skip-duplicate` makes publish idempotent for re-runs.

### D4: npm — publish all packages in packages/

**Decision**: For each directory under `packages/` that contains a `package.json` with `"private": false` (or no private field), run `npm publish`. Initially: `packages/bridge`. As plugin npm packages are added (e.g., `packages/bridge-plugin-http-client`), extend the workflow to iterate over `packages/*/package.json` or a manifest. Each package gets version from tag; pre-release tag uses `--tag preview`.

**Rationale**: Single source of truth: directory structure. Avoid hardcoding each package. A matrix or loop over packages scales with plugin growth.

### D5: Pre-release detection

**Decision**: Pre-release SHALL be detected by tag format: if tag contains `-` (e.g., `v1.1.0-preview`), it is a pre-release. NuGet: version string already includes `-preview`; no extra flag. npm: `--tag preview` (or `--tag next`) so `npm install @agibuild/bridge` gets latest stable. GitHub Release: `prerelease: ${{ contains(github.ref_name, '-') }}`.

**Rationale**: Consistent pre-release handling across all package types. Users opt-in to pre-release via `npm install @agibuild/bridge@preview` or NuGet prerelease feed.

### D6: Release notes from conventional commits

**Decision**: Use `generate_release_notes: true` in `softprops/action-gh-release`. GitHub's default generator uses commits since last tag. For richer conventional-commit formatting, optionally use `conventional-changelog-action` or `git-cliff` to generate a changelog from conventional commits (feat, fix, chore, etc.) and pass it as `body` to the release. For v1, `generate_release_notes: true` suffices; enhance later if needed.

**Rationale**: GitHub's built-in generator is simple and requires no extra deps. Conventional commit tooling can be added in a follow-up for categorized release notes.

### D7: Environment protection

**Decision**: NuGet and npm publish jobs SHALL use GitHub Environments (`nuget`, `npm`) with required secrets (`NUGET_API_KEY`, `NPM_TOKEN`). Environments MAY have protection rules (e.g., require approval for production). Document secret configuration in README or release docs.

**Rationale**: Secrets are not in workflow file. Environment protection adds safety for production releases. Clear docs reduce setup friction.

## Risks / Trade-offs

### R1: Build platform (macOS) for NuGet pack

**Risk**: NuGet pack runs on macOS (for iOS/Android workloads). Some platform-specific assets may differ from Windows/Linux.

**Mitigation**: Current CI uses macOS for full workload coverage. NuGet packages are platform-agnostic for most content. Adapters use RID-specific folders; validate on macOS.

### R2: npm package iteration

**Risk**: Hardcoding `packages/bridge` does not scale when plugin npm packages are added.

**Mitigation**: Refactor to a matrix or script that discovers `packages/*/package.json` and publishes each. Add when second npm package exists.

### R3: Duplicate publish handling

**Risk**: Re-running workflow or accidental duplicate tag push could attempt duplicate publish.

**Mitigation**: `--skip-duplicate` for NuGet. npm: check `npm view` before publish; treat duplicate as warning, not failure. Document idempotent behavior.
