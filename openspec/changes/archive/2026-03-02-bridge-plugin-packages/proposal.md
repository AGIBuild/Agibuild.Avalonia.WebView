## Why

Fulora's bridge architecture enables typed, policy-governed communication between C# and JS. But extending this with reusable capabilities (SQLite access, Auth/SSO, analytics, local storage) requires developers to manually implement both C# `[JsExport]` services and TypeScript consumer code. There is no convention for packaging bridge services as reusable NuGet + npm packages. Establishing this convention unlocks a plugin ecosystem where the community can publish typed bridge capabilities.

**Goal IDs**: G1 (Type-Safe Bridge — ecosystem leverage), E1 (Project Template — plugin discovery and integration)

**ROADMAP justification**: Post-1.0 ecosystem growth. This is a unique differentiator — Electron's Node module ecosystem is untyped IPC; Tauri plugins require Rust. Fulora bridge plugins can deliver compile-time type safety from NuGet install to npm import with zero configuration.

## What Changes

- Define the **Bridge Plugin Convention**: a NuGet package that contains `[JsExport]`/`[JsImport]` interfaces, implementations, and generated TypeScript types
- Define a plugin registration API: `Bridge.UsePlugin<TPlugin>()` that auto-registers all services from the plugin assembly
- Define the npm companion package convention: `@agibuild/bridge-plugin-{name}` with pre-generated TypeScript types
- Provide a plugin scaffold command in CLI: `agibuild add plugin <name>`
- Create a reference plugin implementation as a proof-of-concept (e.g., `Agibuild.Fulora.Plugin.LocalStorage`)

## Non-goals

- Plugin marketplace or registry — NuGet + npm are the distribution channels
- Runtime plugin loading/unloading (hot-plug) — plugins are registered at startup
- Plugin UI components — plugins provide bridge services only, not Avalonia controls
- Versioning compatibility enforcement between NuGet and npm packages — advisory documentation only

## Capabilities

### New Capabilities
- `bridge-plugin-convention`: Convention and runtime API for packaging, discovering, and registering reusable bridge service plugins delivered as NuGet + npm package pairs with compile-time type safety

### Modified Capabilities
- None

## Impact

- `src/Agibuild.Fulora.Core/` — `IBridgePlugin` interface, `UsePlugin<T>()` extension on `IBridgeService`
- `src/Agibuild.Fulora.Runtime/` — plugin assembly scanning, auto-registration logic
- `src/Agibuild.Fulora.Bridge.Generator/` — possible enhancement for plugin-level TypeScript aggregation
- `docs/` — plugin authoring guide
- New project: reference plugin (e.g., `plugins/Agibuild.Fulora.Plugin.LocalStorage/`)
- `templates/` — update CLI scaffold for plugin creation
