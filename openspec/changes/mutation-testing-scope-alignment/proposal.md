## Why

The mutation testing workflow currently runs `BuildAll` before invoking Stryker. `BuildAll` is designed for full-solution analysis and may include platform heads (including iOS test heads), which introduces host/Xcode coupling unrelated to mutation testing scope. This creates avoidable failures in scheduled mutation runs.

The current mutation chain also does not fully align with repository rules for core business scope: `Agibuild.Fulora.Runtime` and `Agibuild.Fulora.AI` should be covered, while provider wrappers and platform-specific integration heads should stay out of mutation scope.

## What Changes

- Make Nuke `MutationTest` the single orchestration entry for mutation CI.
- Remove workflow-level direct Stryker invocation and `BuildAll` dependency from mutation workflow.
- Introduce explicit mutation profiles for `Core`, `Runtime`, and `AI`.
- Add a dedicated `BuildMutationScope` target to build only mutation-relevant projects before mutation runs.
- Normalize Stryker configuration into a single source-of-truth layout with per-profile config files and explicit `mutate` filters.
- Update governance tests to enforce workflow orchestration consistency.

## Non-goals

- Running mutation testing for UI projects and native platform adapters.
- Running mutation testing for provider wrappers (`Agibuild.Fulora.AI.Ollama`, `Agibuild.Fulora.AI.OpenAI`).
- Expanding mutation scope to integration/mobile head projects.
- Changing mutation score thresholds in this change.

## Capabilities

### New Capabilities

- `mutation-testing`: Multi-profile mutation orchestration (Core/Runtime/AI) with deterministic scope and report partitioning.

### Modified Capabilities

- `mutation-testing`: CI execution path switches from workflow-scripted build/stryker chain to Nuke-owned orchestration.

## Impact

- **Workflow**: `.github/workflows/mutation-testing.yml` becomes orchestration-thin and calls Nuke `MutationTest` only.
- **Build system**: `build/Build.Testing.cs` adds mutation profile orchestration and dedicated pre-mutation build scope.
- **Configuration**: Stryker config moves to explicit multi-profile files with strict mutate filters.
- **Tests**: `tests/Agibuild.Fulora.UnitTests/AutomationLaneGovernanceTests.cs` adds regression guards for mutation workflow orchestration boundaries.
