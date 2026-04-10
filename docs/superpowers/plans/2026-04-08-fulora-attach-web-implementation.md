# Fulora Attach Web Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a first-class brownfield CLI path that attaches an existing web app to a Fulora desktop host, persists the wiring in `fulora.json`, and lets `dev`, `generate types`, and `package` reuse that shared configuration without hidden auto-fix behavior.

**Architecture:** Keep the first slice intentionally narrow and explicit. Introduce a small workspace-config layer for the CLI, add `fulora attach web` as the wiring entry point, scaffold only Fulora-owned files (`desktop`, `bridge`, `src/bridge/*`, `fulora.json`), then teach existing commands to consume the saved config before falling back to current heuristics. Manifest/hash checks remain verification-only: `attach web` establishes structure, `generate types` regenerates artifacts, and preflight commands report drift instead of mutating state.

**Tech Stack:** .NET 10, System.CommandLine, System.Text.Json, xUnit, existing Fulora CLI command/test infrastructure, DocFX markdown docs.

## Current implementation status — 2026-04-10

This plan is now **implemented for the current approved slice**.

Completed slices:

- shared `fulora.json` config model and resolver
- `fulora attach web` command registration, validation flow, and scaffolding
- config-first reuse in `dev`, `generate types`, and `package`
- generated/client-facing `createFuloraClient()` + `services.*` ergonomics
- dedicated CLI / docs / governance test projects and broad per-project test coverage

Remaining caveats:

- this plan document was updated after implementation, so the original checkbox workflow was not maintained in real time
- broader DX roadmap items like `fulora doctor` and deeper packaging productization remain follow-up work outside this attach-web slice

Evidence anchors for completed work:

- `src/Agibuild.Fulora.Cli/Commands/AttachCommand.cs`
- `src/Agibuild.Fulora.Cli/Commands/AttachWebScaffolder.cs`
- `src/Agibuild.Fulora.Cli/Commands/FuloraWorkspaceConfig.cs`
- `src/Agibuild.Fulora.Cli/Commands/FuloraWorkspaceConfigResolver.cs`
- `src/Agibuild.Fulora.Cli/Commands/DevCommand.cs`
- `src/Agibuild.Fulora.Cli/Commands/GenerateCommand.cs`
- `src/Agibuild.Fulora.Cli/Commands/PackageCommand.cs`
- `src/Agibuild.Fulora.Bridge.Generator/TypeScriptClientEmitter.cs`
- `tests/Agibuild.Fulora.Cli.UnitTests/`
- `tests/Agibuild.Fulora.Docs.UnitTests/`
- `tests/Agibuild.Fulora.Governance.UnitTests/`

---

## File Map

### CLI command registration

- `src/Agibuild.Fulora.Cli/Program.cs`
  - Register the new top-level `attach` command group.
- `src/Agibuild.Fulora.Cli/Commands/AttachCommand.cs`
  - Own the `attach web` command, options, validation flow, and next-step output.

### Shared workspace configuration

- `src/Agibuild.Fulora.Cli/Commands/FuloraWorkspaceConfig.cs`
  - Define the serializable `fulora.json` shape and path-normalization helpers.
- `src/Agibuild.Fulora.Cli/Commands/FuloraWorkspaceConfigResolver.cs`
  - Locate, load, validate, and save `fulora.json` relative to the working directory.

### Attach-web scaffolding helpers

- `src/Agibuild.Fulora.Cli/Commands/AttachWebScaffolder.cs`
  - Create Fulora-owned directories/files and keep user-owned web app code untouched.
- `src/Agibuild.Fulora.Cli/Commands/NewCommand.cs`
  - Reuse process-launch helpers where attach needs `dotnet new` or other shell execution.

### Existing command integration

- `src/Agibuild.Fulora.Cli/Commands/DevCommand.cs`
  - Prefer `fulora.json` values for web root / desktop project before current auto-detection.
- `src/Agibuild.Fulora.Cli/Commands/GenerateCommand.cs`
  - Prefer configured bridge project and generated output directory before heuristic detection.
- `src/Agibuild.Fulora.Cli/Commands/PackageCommand.cs`
  - Prefer configured desktop project when `--project` is omitted, but keep explicit override behavior.

### Docs

- `README.md`
  - Add the canonical brownfield CLI path once it exists.
- `docs/cli.md`
  - Document `attach web`, `fulora.json`, and the explicit consistency model.
- `docs/articles/bring-your-own-web-app-quickstart.md`
  - Update the quick start to begin with `fulora attach web`.
- `docs/articles/bring-your-own-web-app.md`
  - Update the full brownfield guide to reflect the implemented CLI path.

### Tests

- `tests/Agibuild.Fulora.UnitTests/CliToolTests.cs`
  - CLI help, end-to-end attach-web command behavior, and config-aware command discovery.
- `tests/Agibuild.Fulora.UnitTests/FuloraWorkspaceConfigTests.cs`
  - Focused parser/serializer/path-resolution coverage for `fulora.json`.
- `tests/Agibuild.Fulora.UnitTests/AppDistributionTests.cs`
  - Package-command default project resolution from config.
- `tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs`
  - Guard the new CLI path in first-contact docs.

## Scope Guardrails

- Do not redesign bridge transport/runtime internals.
- Do not silently regenerate artifacts inside `attach web`, `dev`, or `package`.
- Do not rewrite the user’s existing frontend routes, components, or business logic.
- Do not introduce `fulora doctor` integration in this slice.
- Do not attempt framework-specific auto-editing beyond a narrow v1 path (React/Vue + Vite, generic custom dev server).

### Task 1: Add shared `fulora.json` workspace config primitives

**Files:**
- Create: `src/Agibuild.Fulora.Cli/Commands/FuloraWorkspaceConfig.cs`
- Create: `src/Agibuild.Fulora.Cli/Commands/FuloraWorkspaceConfigResolver.cs`
- Create: `tests/Agibuild.Fulora.UnitTests/FuloraWorkspaceConfigTests.cs`

- [x] **Step 1: Write the failing config tests**

Create `tests/Agibuild.Fulora.UnitTests/FuloraWorkspaceConfigTests.cs` with focused tests that require:

```csharp
[Fact]
public void Load_returns_null_when_fulora_json_is_missing()

[Fact]
public void Save_and_load_round_trip_relative_paths()

[Fact]
public void Resolve_from_nested_working_directory_finds_repo_root_config()
```

Also cover that `generatedDir`, `bridge.project`, and `desktop.project` are persisted exactly as repo-relative paths.

- [x] **Step 2: Run the focused tests to verify they fail**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter FuloraWorkspaceConfigTests
```

Expected: FAIL because the config types and resolver do not exist yet.

- [x] **Step 3: Implement the minimal config model and resolver**

Add:

- `FuloraWorkspaceConfig`
  - `Web.Root`
  - `Web.Command`
  - `Web.DevServerUrl`
  - `Web.GeneratedDir`
  - `Bridge.Project`
  - `Desktop.Project`
- `FuloraWorkspaceConfigResolver`
  - find nearest `fulora.json` by walking upward from cwd
  - deserialize with `System.Text.Json`
  - normalize save paths relative to the config root
  - expose helpers to resolve config values to absolute paths for command use

Keep the schema intentionally small and deterministic.

- [x] **Step 4: Re-run the focused tests**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter FuloraWorkspaceConfigTests
```

Expected: PASS

- [x] **Step 5: Commit**

```bash
git add src/Agibuild.Fulora.Cli/Commands/FuloraWorkspaceConfig.cs src/Agibuild.Fulora.Cli/Commands/FuloraWorkspaceConfigResolver.cs tests/Agibuild.Fulora.UnitTests/FuloraWorkspaceConfigTests.cs
git commit -m "Add a shared workspace config for brownfield Fulora apps"
```

### Task 2: Add the `fulora attach web` command contract and validation flow

**Files:**
- Modify: `src/Agibuild.Fulora.Cli/Program.cs`
- Create: `src/Agibuild.Fulora.Cli/Commands/AttachCommand.cs`
- Modify: `tests/Agibuild.Fulora.UnitTests/CliToolTests.cs`

- [x] **Step 1: Write the failing CLI tests for command discovery and validation**

Add or extend tests that require:

```csharp
[Fact]
public async Task Help_shows_attach_command()

[Fact]
public async Task Attach_web_help_shows_web_desktop_bridge_and_framework_options()

[Fact]
public async Task Attach_web_requires_an_existing_web_project_root()
```

Use a temp workspace with and without `package.json` so the failure message is adoption-oriented, for example:

- `Fulora could not find your web project root.`

- [x] **Step 2: Run the focused CLI tests to verify failure**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests"
```

Expected: FAIL because `attach web` is not registered and the validation path does not exist.

- [x] **Step 3: Implement the command shell and option parsing**

Register a new top-level `attach` command group with a `web` subcommand in `Program.cs`.

In `AttachCommand.cs`, add options for:

- `--web` (required)
- `--desktop`
- `--bridge`
- `--framework`
- `--web-command`
- `--dev-server-url`

Validation rules:

- `--web` must point to an existing directory containing `package.json`
- `--framework` accepts a narrow v1 allowlist (`react`, `vue`, `generic`)
- `--desktop` and `--bridge` may be omitted and default to conventional sibling directories
- explicit paths always override inferred defaults

Do not scaffold files yet in this task; only get the command contract, parsing, and user-facing validation in place.

- [x] **Step 4: Re-run the focused CLI tests**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests"
```

Expected: PASS for the new help and validation coverage.

- [x] **Step 5: Commit**

```bash
git add src/Agibuild.Fulora.Cli/Program.cs src/Agibuild.Fulora.Cli/Commands/AttachCommand.cs tests/Agibuild.Fulora.UnitTests/CliToolTests.cs
git commit -m "Add the attach web CLI contract"
```

### Task 3: Scaffold Fulora-owned brownfield wiring and persist `fulora.json`

**Files:**
- Modify: `src/Agibuild.Fulora.Cli/Commands/AttachCommand.cs`
- Create: `src/Agibuild.Fulora.Cli/Commands/AttachWebScaffolder.cs`
- Modify: `tests/Agibuild.Fulora.UnitTests/CliToolTests.cs`

- [x] **Step 1: Write the failing attach-web scaffolding test**

Add a temp-workspace CLI test that:

1. creates an existing web app root with `package.json`
2. runs:

```bash
attach web --web "<web>" --desktop "<desktop-dir>" --bridge "<bridge-dir>" --framework react --web-command "npm run dev" --dev-server-url http://localhost:5173
```

3. asserts that Fulora creates:

- `fulora.json`
- `<web>/src/bridge/client.ts`
- `<web>/src/bridge/services.ts`
- `<web>/src/bridge/generated/`
- `<bridge>/<Name>.Bridge.csproj`
- `<desktop>/<Name>.Desktop.csproj`

and prints next steps containing:

- the existing web command
- `fulora dev`
- `fulora generate types`

- [x] **Step 2: Run the focused attach tests to verify failure**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests"
```

Expected: FAIL because the command does not yet scaffold or persist config.

- [x] **Step 3: Implement the minimal scaffolder**

Create `AttachWebScaffolder.cs` that:

- creates `src/bridge/generated/` if missing
- writes thin hand-authored `client.ts` and `services.ts` entrypoints only when absent
- creates basic bridge and desktop project files only when absent
- writes `fulora.json` using the resolver from Task 1

Important constraints:

- never overwrite existing user web app files without an explicit future flag
- if an existing file conflicts, fail with an actionable message instead of guessing
- do not call `fulora generate types` automatically

- [x] **Step 4: Re-run the focused attach tests**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests"
```

Expected: PASS

- [x] **Step 5: Commit**

```bash
git add src/Agibuild.Fulora.Cli/Commands/AttachCommand.cs src/Agibuild.Fulora.Cli/Commands/AttachWebScaffolder.cs tests/Agibuild.Fulora.UnitTests/CliToolTests.cs
git commit -m "Scaffold brownfield Fulora wiring for existing web apps"
```

### Task 4: Teach existing CLI commands to consume `fulora.json`

**Files:**
- Modify: `src/Agibuild.Fulora.Cli/Commands/DevCommand.cs`
- Modify: `src/Agibuild.Fulora.Cli/Commands/GenerateCommand.cs`
- Modify: `src/Agibuild.Fulora.Cli/Commands/PackageCommand.cs`
- Modify: `tests/Agibuild.Fulora.UnitTests/CliToolTests.cs`
- Modify: `tests/Agibuild.Fulora.UnitTests/AppDistributionTests.cs`

- [x] **Step 1: Write the failing config-aware command tests**

Add tests that require:

- `fulora dev --preflight-only` to use `fulora.json` web/desktop paths when flags are omitted
- `fulora generate types` to use configured bridge project + generated dir when flags are omitted
- `fulora package --preflight-only` to use configured desktop project when `--project` is omitted

Use temp workspaces that intentionally do not satisfy the current heuristic naming rules so the tests prove config reuse, not fallback luck.

- [x] **Step 2: Run the focused tests to verify failure**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests|AppDistributionTests"
```

Expected: FAIL because the commands currently rely on heuristics and explicit flags only.

- [x] **Step 3: Implement config-first resolution**

Update the commands so they resolve values in this order:

1. explicit CLI option
2. `fulora.json`
3. existing heuristic auto-detection

Rules:

- keep existing success/failure messages unless config makes them more actionable
- do not change manifest/hash consistency behavior
- package remains explicit-override-friendly, but config removes unnecessary path repetition

- [x] **Step 4: Re-run the focused tests**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests|AppDistributionTests"
```

Expected: PASS

- [x] **Step 5: Commit**

```bash
git add src/Agibuild.Fulora.Cli/Commands/DevCommand.cs src/Agibuild.Fulora.Cli/Commands/GenerateCommand.cs src/Agibuild.Fulora.Cli/Commands/PackageCommand.cs tests/Agibuild.Fulora.UnitTests/CliToolTests.cs tests/Agibuild.Fulora.UnitTests/AppDistributionTests.cs
git commit -m "Reuse fulora.json across dev, generate, and package"
```

### Task 5: Update brownfield docs and guard the explicit consistency model

**Files:**
- Modify: `README.md`
- Modify: `docs/cli.md`
- Modify: `docs/articles/bring-your-own-web-app-quickstart.md`
- Modify: `docs/articles/bring-your-own-web-app.md`
- Modify: `tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs`

- [x] **Step 1: Write the failing docs-governance assertions**

Extend `DocumentationGovernanceTests.cs` so first-contact docs require:

- the phrase `fulora attach web`
- the canonical brownfield path `attach web -> dev -> package`
- explicit wording that stale bridge artifacts should be fixed with `fulora generate types`, not hidden auto-fix

- [x] **Step 2: Run the focused docs tests to verify failure**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter DocumentationGovernanceTests
```

Expected: FAIL because docs do not yet reflect the implemented attach-web path.

- [x] **Step 3: Update docs to match the shipped CLI**

Update:

- `README.md` with an “Already have a web app?” quick path
- `docs/cli.md` with `attach web` syntax and `fulora.json`
- the BYO quickstart/full guide so they start with attach-web and keep the services-first bridge guidance

Make sure docs clearly state:

- `attach web` wires the project
- `generate types` regenerates artifacts explicitly
- `dev/package --preflight-only` verify consistency and report drift

- [x] **Step 4: Re-run the focused docs tests**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter DocumentationGovernanceTests
```

Expected: PASS

- [x] **Step 5: Commit**

```bash
git add README.md docs/cli.md docs/articles/bring-your-own-web-app-quickstart.md docs/articles/bring-your-own-web-app.md tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs
git commit -m "Document the attach-web brownfield path"
```

### Task 6: Run full relevant verification and prepare merge-ready evidence

**Files:**
- Modify: none (verification only unless failures require fixes)

- [x] **Step 1: Run the unit test suite for the touched surfaces**

Run:

```bash
dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests|FuloraWorkspaceConfigTests|AppDistributionTests|DocumentationGovernanceTests"
```

Expected: PASS

- [x] **Step 2: Run a docs build smoke check**

Run:

```bash
dotnet tool restore
dotnet docfx docs/docfx.json
```

Expected: PASS with no broken brownfield-path references.

- [x] **Step 3: Run a repository diff review**

Run:

```bash
git diff --check
git status --short
```

Expected:

- no whitespace/conflict issues
- only intended CLI/docs/test files changed

- [x] **Step 4: Create the final integration commit**

```bash
git add src/Agibuild.Fulora.Cli README.md docs/cli.md docs/articles/bring-your-own-web-app-quickstart.md docs/articles/bring-your-own-web-app.md tests/Agibuild.Fulora.UnitTests
git commit -m "Productize the brownfield Fulora attach-web workflow"
```

Use the Lore commit format with explicit notes about:

- shared config reuse
- no hidden auto-fix behavior
- config-first resolution order
- verification coverage and remaining gaps
