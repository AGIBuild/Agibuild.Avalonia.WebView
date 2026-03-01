## Why

Phase 9 M9.3 requires `@agibuild/bridge` to be publishable to the npm registry so frontend developers can `npm install @agibuild/bridge`. The package exists at `packages/bridge/` with working build, governance validation, and package-manager parity checks — but lacks publication metadata and a nuke build target to execute `npm publish`.

## What Changes

- Add publication metadata to `packages/bridge/package.json` (repository, homepage, keywords, author, bugs)
- Add `NpmPublish` target to `Build.Packaging.cs` that runs `npm publish` with `NPM_TOKEN` authentication
- Wire `NpmPublish` into the existing `Publish` target dependency chain
- Update ROADMAP M9.3 → Done

## Capabilities

### New Capabilities

- `npm-bridge-publication`: Automated npm publication of `@agibuild/bridge` via nuke build target

### Modified Capabilities

_None_

## Non-goals

- Changing the bridge client runtime code
- Adding CJS dual-format support (ESM-only is intentional)
- Versioning automation (manual version bump for now; future M9.6 concern)

## Impact

- `packages/bridge/package.json` — Publication metadata added
- `build/Build.Packaging.cs` — `NpmPublish` target added
- `openspec/ROADMAP.md` — M9.3 → Done
