## 1. OpenSpec artifacts

- [x] 1.1 Create proposal for mutation orchestration and scope alignment
- [x] 1.2 Create design describing Nuke single-entry orchestration and profile model
- [x] 1.3 Create delta spec requirements for workflow/build/config/test boundaries

## 2. Build orchestration refactor

- [ ] 2.1 Add `BuildMutationScope` target in `build/Build.Testing.cs`
- [ ] 2.2 Add mutation profile model (core/runtime/ai) in `build/Build.Testing.cs`
- [ ] 2.3 Refactor `MutationTest` to depend on `BuildMutationScope` and iterate profiles

## 3. Stryker scope normalization

- [ ] 3.1 Split root Stryker config into profile configs (`core`, `runtime`, `ai`)
- [ ] 3.2 Add explicit `mutate` filters for each profile
- [ ] 3.3 Remove duplicate/unused mutation config source from `tests/Agibuild.Fulora.UnitTests`

## 4. Workflow single-entry enforcement

- [ ] 4.1 Update `.github/workflows/mutation-testing.yml` to call only `MutationTest`
- [ ] 4.2 Remove workflow-level `BuildAll` and direct `dotnet stryker` orchestration
- [ ] 4.3 Keep mutation report upload path aligned with Nuke outputs

## 5. Governance + verification

- [ ] 5.1 Update `AutomationLaneGovernanceTests` to assert mutation workflow orchestration boundaries
- [ ] 5.2 Run unit governance tests
- [ ] 5.3 Run `./build.sh --target MutationTest --configuration Release` for end-to-end validation
