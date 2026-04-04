# Fulora DX Primary Path Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Milestone 1 of the Fulora DX decision document by making the default user story feel like `new -> dev -> package`, restructuring first-contact docs around application builders, and introducing the initial `package --profile` workflow.

**Architecture:** Keep the first slice intentionally narrow. Do not attempt P2 automation or P3 ecosystem work here. Reuse the existing CLI command surface, existing template defaults, and existing documentation governance tests, then tighten the defaults and first-contact narrative around one app-builder path.

**Tech Stack:** .NET 10, System.CommandLine, xUnit, DocFX markdown docs, existing Fulora CLI/template infrastructure.

---

## File Map

### CLI and packaging behavior

- `src/Agibuild.Fulora.Cli/Program.cs`
  - Root command description and subcommand registration.
- `src/Agibuild.Fulora.Cli/Commands/NewCommand.cs`
  - `fulora new` defaults and help text.
- `src/Agibuild.Fulora.Cli/Commands/PackageCommand.cs`
  - `fulora package` options and packaging workflow.
- `src/Agibuild.Fulora.Cli/Commands/PackageProfileDefaults.cs`
  - New helper for named packaging profiles and their resolved defaults.

### Documentation

- `README.md`
  - Repo-level first contact and quick path.
- `docs/index.md`
  - Role-based docs homepage.
- `docs/articles/getting-started.md`
  - App-builder onboarding path.
- `docs/cli.md`
  - CLI framing and command priority.
- `docs/shipping-your-app.md`
  - Productized packaging story and profile-based release path.

### Tests

- `tests/Agibuild.Fulora.UnitTests/CliToolTests.cs`
  - CLI help text and scaffold workflow expectations.
- `tests/Agibuild.Fulora.UnitTests/AppDistributionTests.cs`
  - `PackageCommand` options and profile behavior.
- `tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs`
  - Role-based docs entry and guarded first-contact structure.

## Scope Guardrails

- This plan implements only Milestone 1 from [2026-04-04-fulora-dx-decision-document.md](/Users/Hongwei.Xi/projects/Fulora/docs/superpowers/specs/2026-04-04-fulora-dx-decision-document.md).
- Do not add `fulora doctor` in this slice.
- Do not automate `generate types` in this slice.
- Do not restructure the physical template layout in this slice.
- Do not delete advanced commands such as `generate`, `add`, `search`, or plugin commands. Reduce their prominence instead.

### Task 1: Make `fulora new` match the primary app path

**Files:**
- Modify: `src/Agibuild.Fulora.Cli/Commands/NewCommand.cs`
- Modify: `src/Agibuild.Fulora.Cli/Program.cs`
- Test: `tests/Agibuild.Fulora.UnitTests/CliToolTests.cs`

- [ ] **Step 1: Write the failing CLI help tests**

Add or update tests to require:

```csharp
[Fact]
public async Task New_command_help_shows_frontend_but_not_required_shell_preset()
{
    var (stdout, _, exitCode) = await RunCliAsync("new --help");

    Assert.Equal(0, exitCode);
    Assert.Contains("--frontend", stdout);
    Assert.DoesNotContain("Required", stdout[(stdout.IndexOf("--shell-preset", StringComparison.Ordinal))..], StringComparison.OrdinalIgnoreCase);
}

[Fact]
public async Task New_command_can_be_described_by_primary_path()
{
    var (stdout, _, exitCode) = await RunCliAsync("--help");

    Assert.Equal(0, exitCode);
    Assert.Contains("scaffold", stdout, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("develop", stdout, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("package", stdout, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run focused CLI tests to verify failure**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests"`
Expected: FAIL because `new` currently requires `--shell-preset` and the help text still exposes it as part of the default workflow.

- [ ] **Step 3: Implement the new-command default behavior**

Update `NewCommand.Create()` so that:

- `--frontend` stays required
- `--shell-preset` becomes optional
- default shell preset resolves to `app-shell`
- success text keeps the next steps to:

```text
cd <name>
fulora dev
```

Also update the root command description in `Program.cs` so the top-level CLI story emphasizes app creation, development, and packaging.

- [ ] **Step 4: Re-run focused CLI tests**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Agibuild.Fulora.Cli/Program.cs src/Agibuild.Fulora.Cli/Commands/NewCommand.cs tests/Agibuild.Fulora.UnitTests/CliToolTests.cs
git commit -m "feat: align new command with primary app path"
```

### Task 2: Add named packaging profiles to `fulora package`

**Files:**
- Create: `src/Agibuild.Fulora.Cli/Commands/PackageProfileDefaults.cs`
- Modify: `src/Agibuild.Fulora.Cli/Commands/PackageCommand.cs`
- Test: `tests/Agibuild.Fulora.UnitTests/AppDistributionTests.cs`
- Test: `tests/Agibuild.Fulora.UnitTests/CliToolTests.cs`

- [ ] **Step 1: Write the failing packaging-profile tests**

Add tests that require:

```csharp
[Fact]
public void PackageCommand_creates_valid_command_with_profile_option()
{
    var command = PackageCommand.Create();
    var optionNames = command.Options.Select(o => o.Name).ToHashSet();
    Assert.Contains("--profile", optionNames);
}

[Fact]
public void ResolveProfile_desktop_public_sets_stable_channel_defaults()
{
    var profile = PackageProfileDefaults.Resolve("desktop-public");
    Assert.Equal("stable", profile.Channel);
    Assert.False(profile.Notarize);
}
```

And add a CLI help assertion:

```csharp
[Fact]
public async Task Package_command_help_shows_profile_examples()
{
    var (stdout, _, exitCode) = await RunCliAsync("package --help");
    Assert.Equal(0, exitCode);
    Assert.Contains("--profile", stdout);
    Assert.Contains("desktop-public", stdout);
}
```

- [ ] **Step 2: Run focused packaging tests to verify failure**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "AppDistributionTests|CliToolTests"`
Expected: FAIL because `PackageCommand` does not yet expose `--profile` or any named profile defaults.

- [ ] **Step 3: Implement profile resolution with minimal defaults**

Create `PackageProfileDefaults.cs` with a tiny immutable model such as:

```csharp
internal sealed record PackageProfile(string Name, string Channel, string? Runtime, bool Notarize);
```

Support these first profiles only:

- `desktop-internal`
- `desktop-public`
- `mac-notarized`

Then update `PackageCommand.Create()` so that:

- `--profile` is optional
- explicit flags still win over profile defaults
- `desktop-public` resolves to stable defaults
- `mac-notarized` enables notarization-oriented defaults
- help text shows profile names as the recommended first path

- [ ] **Step 4: Re-run focused packaging tests**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "AppDistributionTests|CliToolTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Agibuild.Fulora.Cli/Commands/PackageCommand.cs src/Agibuild.Fulora.Cli/Commands/PackageProfileDefaults.cs tests/Agibuild.Fulora.UnitTests/AppDistributionTests.cs tests/Agibuild.Fulora.UnitTests/CliToolTests.cs
git commit -m "feat: add package profiles to cli"
```

### Task 3: Guard the new docs entry model with governance tests

**Files:**
- Modify: `tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs`

- [ ] **Step 1: Write failing docs-governance assertions**

Add assertions that require `docs/index.md` to contain role-based entry points and require `README.md` to keep a primary-path quick start.

Suggested assertions:

```csharp
[Fact]
public void Docs_index_exposes_role_based_getting_started_paths()
{
    var repoRoot = FindRepoRoot();
    var content = File.ReadAllText(Path.Combine(repoRoot, "docs", "index.md"));

    Assert.Contains("I am building an app", content, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("I am building a plugin", content, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("I am working on the platform", content, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void Readme_keeps_primary_path_quick_start()
{
    var repoRoot = FindRepoRoot();
    var content = File.ReadAllText(Path.Combine(repoRoot, "README.md"));

    Assert.Contains("fulora new", content);
    Assert.Contains("fulora dev", content);
    Assert.Contains("fulora package", content);
}
```

- [ ] **Step 2: Run governance tests to verify failure**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "DocumentationGovernanceTests"`
Expected: FAIL because the current docs homepage is platform-oriented and the current README does not treat `package` as part of the core quick path.

- [ ] **Step 3: Keep the new assertions small and stable**

When editing the tests, avoid brittle exact-string snapshots. Assert only the protected structure:

- role-based docs entry exists
- README quick path includes `new`, `dev`, `package`
- existing platform document governance keeps passing

- [ ] **Step 4: Re-run governance tests**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "DocumentationGovernanceTests"`
Expected: FAIL until the docs changes from Tasks 4-6 are applied. This step confirms the guardrails are active.

### Task 4: Rewrite the README around the app-builder journey

**Files:**
- Modify: `README.md`
- Test: `tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs`

- [ ] **Step 1: Replace the current front-loaded platform framing**

Restructure the top of `README.md` so the first reader sees:

1. what Fulora is in one sentence
2. the quick path:

```bash
fulora new MyApp --frontend react
cd MyApp
fulora dev
fulora package --profile desktop-public
```

3. one short explanation of app services / native capabilities
4. links to deeper docs

- [ ] **Step 2: Reduce first-screen platform density**

Move or compress early mentions of:

- four-layer platform model
- capability tiers
- broad ecosystem inventory
- governance language

Keep them linked, but no longer make them compete with onboarding above the fold.

- [ ] **Step 3: Re-run docs governance tests**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "DocumentationGovernanceTests"`
Expected: Partial progress; README assertions should now pass while role-based docs assertions may still fail.

- [ ] **Step 4: Commit**

```bash
git add README.md tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs
git commit -m "docs: rewrite readme around primary app path"
```

### Task 5: Rebuild `docs/index.md` and `getting-started.md` around roles and the main path

**Files:**
- Modify: `docs/index.md`
- Modify: `docs/articles/getting-started.md`
- Test: `tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs`

- [ ] **Step 1: Rewrite `docs/index.md` into role-based entry sections**

Make the top-level flow look like:

```md
## I am building an app
- Getting Started
- Dev Workflow
- Calling Native Services
- Packaging

## I am building a plugin
- Plugin Basics
- Metadata
- Testing
- Publishing

## I am working on the platform
- Architecture
- Governance
- Adapters
- Runtime internals
```

Keep the required platform documents table intact so existing governance tests still pass.

- [ ] **Step 2: Rewrite `getting-started.md` to minimize bridge ceremony on day one**

Reframe the article so the sequence is:

1. install CLI and template
2. run `fulora new`
3. run `fulora dev`
4. explain that Fulora app services use the bridge underneath
5. link advanced bridge details later

Do not remove bridge coverage entirely. Move it after the user already has a running app.

- [ ] **Step 3: Re-run governance tests**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "DocumentationGovernanceTests"`
Expected: PASS for the role-based docs entry assertions and existing platform-doc assertions.

- [ ] **Step 4: Commit**

```bash
git add docs/index.md docs/articles/getting-started.md tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs
git commit -m "docs: rebuild docs entry for application builders"
```

### Task 6: Update CLI and shipping docs to match the new product story

**Files:**
- Modify: `docs/cli.md`
- Modify: `docs/shipping-your-app.md`
- Test: `tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs`
- Test: `tests/Agibuild.Fulora.UnitTests/CliToolTests.cs`
- Test: `tests/Agibuild.Fulora.UnitTests/AppDistributionTests.cs`

- [ ] **Step 1: Rewrite the CLI docs so commands are not presented at one level**

Change `docs/cli.md` so the opening section shows:

- primary path: `new`, `dev`, `package`
- advanced workflows: `generate types`, `add service`, `add plugin`, `search`, `list plugins`

Use the current command names that actually exist in code. Do not document commands that are not implemented.

- [ ] **Step 2: Rewrite shipping docs around named profiles**

Change `docs/shipping-your-app.md` so the first packaging example is:

```bash
fulora package --project ./src/MyApp.Desktop/MyApp.Desktop.csproj \
  --profile desktop-public \
  --version 1.0.0
```

Then explain profile meaning before exposing raw signing/notarization flags.

- [ ] **Step 3: Re-run focused CLI and packaging tests**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests|AppDistributionTests|DocumentationGovernanceTests"`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add docs/cli.md docs/shipping-your-app.md tests/Agibuild.Fulora.UnitTests/CliToolTests.cs tests/Agibuild.Fulora.UnitTests/AppDistributionTests.cs tests/Agibuild.Fulora.UnitTests/DocumentationGovernanceTests.cs
git commit -m "docs: align cli and shipping guides with primary path"
```

### Task 7: Final verification for the Milestone 1 slice

**Files:**
- Modify: any touched files required to fix final verification failures

- [ ] **Step 1: Run the focused unit suite**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj --filter "CliToolTests|AppDistributionTests|DocumentationGovernanceTests"`
Expected: PASS

- [ ] **Step 2: Run the full unit test project if the focused suite is clean**

Run: `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj`
Expected: PASS

- [ ] **Step 3: Review the final diff before handoff**

Run: `git diff --stat HEAD~6..HEAD`
Expected: only CLI, docs, and tests related to the Milestone 1 slice

- [ ] **Step 4: Commit any verification fixes**

```bash
git add README.md docs src/Agibuild.Fulora.Cli tests/Agibuild.Fulora.UnitTests
git commit -m "chore: finish dx primary path milestone"
```

## Notes For The Executor

- Prefer minimal code changes in CLI behavior. This slice is mostly about defaulting and framing, not inventing a new packaging engine.
- Keep advanced commands intact. The goal is de-emphasis, not removal.
- If `PackageCommand` starts to grow, move profile resolution into `PackageProfileDefaults.cs` instead of adding more branching in the command body.
- Keep docs governance assertions structural and audience-focused, not copy-sensitive.
- Do not start P2 work from this plan. `generate types` automation, doctor checks, and template layout restructuring belong in follow-up plans.
