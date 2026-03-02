# Release Checklist

Step-by-step guide for publishing a new Agibuild.Fulora release (NuGet + npm + GitHub Release).

## Prerequisites

### GitHub Secrets Configuration

| Secret | Environment | Description |
|--------|-------------|-------------|
| `NUGET_API_KEY` | `nuget` | NuGet.org API key with push permissions for `Agibuild.Fulora.*` packages |
| `NPM_TOKEN` | `npm` | npm access token with publish permissions for `@agibuild` scope |

### Local Verification

Before creating a release tag, verify locally:

```bash
# Run full test suite
nuke Test

# Run coverage (line ≥ 96%, branch ≥ 95%)
nuke Coverage

# Pack and validate NuGet packages
nuke ValidatePackage

# Build npm bridge package
cd packages/bridge && npm ci && npm run build
```

## Creating a Release

### 1. Decide the version

- **Stable**: `v1.0.0`, `v1.1.0`, `v2.0.0` (semver, no suffix)
- **Pre-release**: `v1.1.0-preview`, `v2.0.0-rc.1` (semver with suffix)

MinVer derives the NuGet version from the git tag automatically.
The npm version is extracted from the tag in CI and set before publish.

### 2. Create and push the tag

```bash
git tag v1.0.0
git push origin v1.0.0
```

### 3. Monitor the release workflow

The `Release` workflow (`.github/workflows/release.yml`) triggers automatically:

1. **Build job** (macOS): Compiles, runs tests, validates packages
2. **Publish job** (parallel): Pushes `.nupkg` to NuGet.org
3. **npm-publish job** (parallel): Publishes `@agibuild/bridge` to npm
4. **GitHub Release job** (parallel): Creates a GitHub Release with auto-generated notes

### 4. Post-publish verification

```bash
# Verify NuGet
dotnet package search Agibuild.Fulora.Avalonia --exact-match

# Verify npm
npm info @agibuild/bridge

# Verify GitHub Release
gh release view v1.0.0
```

## Troubleshooting

| Issue | Resolution |
|-------|------------|
| NuGet push fails with 409 | Package already exists at that version. `--skip-duplicate` handles this. |
| npm publish fails with 409 | Version already published. The `\|\| true` fallback prevents workflow failure. |
| npm publish fails with 401 | `NPM_TOKEN` is missing or expired. Regenerate token and update the GitHub secret. |
| Build job fails | Check test results artifact. Fix issues and re-tag (delete old tag first if needed). |
