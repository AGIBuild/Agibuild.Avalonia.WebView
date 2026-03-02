## Context

Agibuild.Fulora provides a `dotnet new agibuild-hybrid` template with framework choices (react, vue, vanilla). TypeScript bridge declarations are emitted by `Agibuild.Fulora.Bridge.Generator` at compile time. Developers currently:
- Run `dotnet new agibuild-hybrid` manually with correct parameters
- Build the Bridge project to emit `.d.ts` files
- Start Vite (`npm run dev`) and Avalonia desktop app in separate terminals
- Manually create C# bridge interfaces, implementations, and TS proxies when adding services

**Gap**: No single entry point for common workflows. DX suffers from fragmented commands and documentation.

**Phase alignment**: E1 (Project Template), E2 (Dev Tooling).

## Goals / Non-Goals

**Goals:**
- Provide `agibuild` as a .NET global tool installable via `dotnet tool install -g Agibuild.Fulora.Cli`
- Implement `new`, `generate types`, `dev`, and `add service` commands
- Integrate with existing template and bridge generator; do not duplicate logic

**Non-Goals:**
- Replacing or forking `dotnet new` — delegate to it
- Bundling Node.js or Vite — assume user has them installed
- IDE plugins or deep integration with VS/Rider

## Decisions

### D1: Distribution as .NET global tool

**Choice**: Package as `DotnetToolManifest` and publish to NuGet. Install via `dotnet tool install -g Agibuild.Fulora.Cli`.

**Alternatives considered**:
- Standalone executable: Cross-platform complexity, larger binary
- npm package: Diverges from .NET ecosystem, requires Node for install

**Rationale**: Aligns with .NET tooling conventions. Users already have `dotnet`; no extra runtime.

### D2: `agibuild new` delegates to dotnet new

**Choice**: Invoke `dotnet new agibuild-hybrid -n <name> --framework <frontend>` (or equivalent template parameters). Map `--frontend react|vue|svelte` to template `--framework` if needed.

**Alternatives considered**:
- Custom scaffold logic: Duplicates template, maintenance burden
- Wrapper script only: Less discoverable, no cross-platform consistency

**Rationale**: Single source of truth. Template updates flow through automatically.

### D3: `agibuild generate types` invokes build + extraction

**Choice**: Build the Bridge project (or solution) and extract emitted TypeScript declarations from the build output or source generator artifacts. Write to the web project's types directory.

**Alternatives considered**:
- Standalone reflection-based generator: Duplicates Bridge.Generator logic
- MSBuild target only: Not invokable without full `dotnet build`; CLI gives explicit UX

**Rationale**: Reuse existing generator. CLI provides a dedicated command for "regenerate types" workflow.

### D4: `agibuild dev` runs Vite and Avalonia in parallel

**Choice**: Start `npm run dev` (or `npx vite`) in the web project directory and `dotnet run` for the Desktop project. Use process management to run both; forward signals for graceful shutdown.

**Alternatives considered**:
- Sequential: Poor DX, user must run two terminals
- Custom dev server: Overkill, Vite already handles HMR

**Rationale**: One command for "run everything". Assumes standard Vite setup in template.

### D5: `agibuild add service` scaffolds three files

**Choice**: Generate (1) C# interface with `[JsExport]` or `[JsImport]`, (2) C# implementation, (3) TS proxy/stub. Place files in Bridge and web projects per template conventions.

**Alternatives considered**:
- Only C# interface: User still hand-writes implementation and TS
- Interactive wizard: More complex, slower to implement

**Rationale**: Covers the full bridge service lifecycle. Non-interactive for scripting.

## Risks / Trade-offs

- **[Risk] Node/npm not installed** → `agibuild dev` and `generate types` (if web build needed) fail. Document prerequisite.
- **[Risk] Wrong working directory** → Commands fail or target wrong project. Require run from solution root or detect project layout.
- **[Trade-off] Svelte support** → Template may not yet support `--framework svelte`. CLI can accept the flag and pass through; template will fail until supported.
- **[Trade-off] Single dev server port** → `agibuild dev` assumes default Vite port. Override via env or flag if needed later.
