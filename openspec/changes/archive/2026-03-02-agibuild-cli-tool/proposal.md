## Why

Agibuild.Fulora has a `dotnet new agibuild-hybrid` template but developers still run multiple manual steps: scaffold project, generate bridge TypeScript types, start Vite and Avalonia separately, and hand-write bridge services. A unified CLI improves developer experience and reduces setup friction.

**Goal alignment**: E1 (Project Template), E2 (Dev Tooling).

## What Changes

- Ship `agibuild` as a .NET global tool (`dotnet tool install -g Agibuild.Fulora.Cli`)
- Add four commands: `new`, `generate types`, `dev`, `add service`
- `agibuild new` wraps `dotnet new agibuild-hybrid` with frontend choice
- `agibuild generate types` runs TypeScript type generation from C# bridge assemblies
- `agibuild dev` starts Vite dev server and Avalonia desktop app together
- `agibuild add service` scaffolds a new bridge service (C# interface, implementation, TS proxy)

## Non-goals

- Replacing `dotnet new` — the CLI delegates to it
- Bundling Vite or Node.js — assumes user has Node/npm installed
- IDE integration (VS/Rider) — out of scope for initial release

## Capabilities

### New Capabilities
- `cli-commands`: Unified `agibuild` CLI with `new`, `generate types`, `dev`, and `add service` commands

## Impact

- New project: `src/Agibuild.Fulora.Cli/` (console app, `DotnetToolManifest` package type)
- NuGet: `Agibuild.Fulora.Cli` published alongside existing packages
- Docs: update getting-started to recommend `agibuild new` and `agibuild dev`
