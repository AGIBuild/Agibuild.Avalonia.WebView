## Why

All five roadmap phases are complete (Phase 0-5 ✅). The project has 915 tests, 95.87% line coverage, and a comprehensive web-first foundation. However, the version is still `v0.1.15-preview` and all platforms are marked "Preview". The gap between delivered maturity and published version signal undermines adoption confidence. A formal 1.0.0 stable release is the next strategic step to convert technical completeness into ecosystem credibility.

This aligns with **E1** (Project Template for real-world use), **G3** (Secure by Default — audited API surface), and the Phase 3 GA Release deliverable (3.5-3.8) which defined release readiness criteria.

## What Changes

- **API surface freeze audit**: Inventory all public types/methods, flag any that should be `[Obsolete]` or removed before 1.0, ensure naming conventions are consistent.
- **Version strategy switch**: Update MinVer/CI to produce `1.0.0` stable versions instead of `0.1.x-preview`.
- **README metrics refresh**: Sync test counts (766 unit + 149 integration = 915), coverage (95.87%), phase status, and architecture diagram to current state.
- **CHANGELOG generation**: Create a structured changelog covering Phase 0-5 highlights for the 1.0.0 release.
- **Release automation validation**: Ensure `nuke CiPublish` pipeline produces correct stable NuGet packages with proper metadata.

## Non-goals

- No new features or capabilities — this is a stabilization release.
- No API redesign — only auditing and marking pre-existing issues.
- No platform-specific gap filling — platform parity work belongs in a separate change.

## Capabilities

### New Capabilities
- `release-versioning-strategy`: Defines version progression rules from preview to stable, MinVer configuration, and CI release gate criteria.

### Modified Capabilities
- `api-surface-review`: Add 1.0 freeze requirements — public API inventory completeness, naming convention enforcement, obsolescence policy for pre-1.0 experimental APIs.
- `build-pipeline-resilience`: Add stable release pipeline requirements — version tag-driven release, NuGet stable package metadata validation, CHANGELOG artifact generation.

## Impact

- `build/Build.cs` and related partials — version configuration
- `Directory.Build.props` / `.csproj` files — MinVer settings
- `README.md` — metrics and status refresh
- `CHANGELOG.md` — new file
- CI workflow (`.github/workflows/`) — release trigger configuration
- OpenSpec `ROADMAP.md` — version evidence update
