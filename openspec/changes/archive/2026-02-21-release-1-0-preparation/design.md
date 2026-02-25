## Context

Phase 0-5 are fully completed. The project has 915 tests (766 unit + 149 integration), 95.87% line coverage, and a comprehensive governance framework. However, the published version is `v0.1.15-preview`, all platforms are "Preview", and the README contains stale metrics. The project needs a deliberate transition from "preview quality" to "stable 1.0" signal.

MinVer is used for versioning. Currently, no git tags enforce a `1.0.0` floor. The CI pipeline (`nuke Ci` / `nuke CiPublish`) is fully functional but has not been validated for stable (non-prerelease) NuGet metadata.

## Goals / Non-Goals

**Goals:**
- Establish a repeatable version progression from `1.0.0-preview.x` → `1.0.0`
- Ensure README reflects accurate, current project metrics
- Produce a CHANGELOG that documents Phase 0-5 for 1.0 announcement
- Validate `nuke CiPublish` produces correct stable package metadata
- Add a governance test ensuring README metrics stay current

**Non-Goals:**
- No API redesign — only auditing existing surface
- No new features or capabilities
- No documentation site rebuild — only README and CHANGELOG

## Decisions

### D1: Version floor via git tag

**Choice**: Create a `v1.0.0-preview.1` git tag to set MinVer floor, then use `v1.0.0` tag for stable release.

**Rationale**: MinVer derives versions from the nearest ancestor tag. Setting a `v1.0.0-preview.1` tag immediately moves all builds into the 1.0.x range. The final `v1.0.0` tag produces the stable release. No `Directory.Build.props` changes needed.

**Alternative considered**: Hardcode version in `.csproj` — rejected because MinVer is already configured and tag-based flow is the standard practice.

### D2: README freshness governance

**Choice**: Add a governance test that asserts README contains current test counts and coverage percentage, sourced from the same TRX/Cobertura files used by `PhaseCloseoutSnapshot`.

**Rationale**: README metrics have drifted twice already (742→766 unit, 146→149 integration). A governance test prevents future drift. The test reads README.md and asserts patterns like `Unit: NNN` match latest test results.

**Alternative considered**: Manual updates — rejected because drift is inevitable.

### D3: CHANGELOG format

**Choice**: Keep-a-Changelog format (`CHANGELOG.md`) with one `## [1.0.0]` section summarizing Phase 0-5 highlights grouped by category (Added, Changed).

**Rationale**: Industry standard format, parseable by tooling. A single release section is appropriate since this is the first stable release.

### D4: Package metadata validation for stable release

**Choice**: Extend the existing `ValidatePackage` target to assert that stable (non-prerelease) packages have correct metadata: no `-preview` in description, proper license expression, valid project URL.

**Rationale**: Stable NuGet packages have higher metadata standards than previews. The existing `ValidatePackage` target already inspects `.nuspec` — extending it is minimal effort.

## Risks / Trade-offs

- **[Risk]** Tagging `v1.0.0` is irreversible for SemVer progression → Mitigation: Use `v1.0.0-preview.1` first, validate everything, then tag `v1.0.0` only after full CI pass.
- **[Risk]** README governance test could be brittle if format changes → Mitigation: Use regex patterns that tolerate formatting variations.
