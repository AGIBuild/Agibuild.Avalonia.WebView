## Why

All Phase 9 prerequisites are complete (M9.1–M9.6). The framework is ready for 1.0 stable release: API surface is frozen, npm package is publishable, benchmarks are baselined, changelog and migration guide exist. This change bumps versions to 1.0.0 and validates the release gate.

## What Changes

- Update `Directory.Build.props` MinVerMinimumMajorMinor from `0.1` to `1.0`
- Update `packages/bridge/package.json` version from `0.1.0` to `1.0.0`
- Update ROADMAP M9.7 → Done
- Tag `v1.0.0` (triggers MinVer to produce `1.0.0` for NuGet packages)

## Capabilities

### Modified Capabilities

_None (version bump only)_

## Non-goals

- Adding features or changing APIs
- Publishing to NuGet/npm (separate CI step triggered by tag)

## Impact

- `Directory.Build.props` — MinVer minimum version updated
- `packages/bridge/package.json` — Version bumped
- `openspec/ROADMAP.md` — M9.7 → Done
