## Why

Fulora has a working plugin convention (`IBridgePlugin`, `UsePlugin<T>`, NuGet+npm dual distribution) with one reference plugin (LocalStorage). However, developers cannot **discover** available plugins — there is no search, no catalog, no metadata convention. Without discoverability, the plugin ecosystem cannot grow beyond what the core team builds. This is the single biggest blocker to community adoption and ecosystem flywheel.

**Goal alignment**: Phase 11 M11.2 (Plugin Registry & Discovery). Advances E1 (template/tooling DX) by making plugins findable, and G1 (Bridge) by expanding the bridge service library.

## What Changes

- Define a **NuGet tag convention** for Fulora plugins: `fulora-plugin` tag + structured NuGet description metadata (service list, bridge interface names, platform compatibility)
- Add `fulora search [query]` CLI command that queries NuGet.org for packages tagged `fulora-plugin`, parses metadata, and displays results with name, description, version, service list, and install instructions
- Add `fulora add plugin <package-name>` CLI command that installs both the NuGet package and its companion npm package in one step
- Define a **plugin manifest convention** (`fulora-plugin.json`) embedded in NuGet packages with structured metadata: services exposed, npm companion package name, minimum Fulora version, platform compatibility
- Update the reference `LocalStorage` plugin to include the manifest and tag convention
- Add `fulora list plugins` to show installed plugins in the current project

## Capabilities

### New Capabilities
- `plugin-registry-discovery`: Plugin search, catalog metadata convention, CLI commands for search/add/list

### Modified Capabilities
- `bridge-plugin-convention`: Add manifest file requirement and NuGet tag convention to existing plugin spec
- `cli-commands`: Add `search`, `add plugin`, and `list plugins` commands

## Non-goals

- Self-hosted registry server — we leverage NuGet.org and npmjs.com as existing registries
- Plugin marketplace UI — CLI-first, a web catalog can come later
- Plugin quality scoring or certification — out of scope for this change
- Automatic plugin compatibility verification at runtime

## Impact

- `Agibuild.Fulora.Cli`: New commands (search, add plugin, list plugins)
- `Agibuild.Fulora.Plugin.LocalStorage`: Add manifest and NuGet tags
- `packages/bridge-plugin-local-storage`: Update package.json with convention metadata
- New spec: `openspec/specs/plugin-registry-discovery/spec.md`
- Delta spec: `openspec/specs/bridge-plugin-convention/spec.md`, `openspec/specs/cli-commands/spec.md`
