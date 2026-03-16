## ADDED Requirements

### Requirement: Mutation workflow uses Nuke as single orchestration entry
The mutation testing GitHub workflow SHALL invoke Nuke `MutationTest` as its only mutation orchestration command.

#### Scenario: Workflow runs mutation testing
- **WHEN** `.github/workflows/mutation-testing.yml` executes
- **THEN** it MUST call `./build.sh --target MutationTest --configuration Release`
- **AND** it MUST NOT call `BuildAll` in that workflow
- **AND** it MUST NOT directly call `dotnet stryker` in that workflow

### Requirement: Mutation pre-build scope is isolated from full-solution build
The build system SHALL provide a dedicated pre-mutation build target that only builds mutation-relevant projects.

#### Scenario: Mutation target prepares build artifacts
- **WHEN** `MutationTest` runs
- **THEN** it MUST depend on `BuildMutationScope`
- **AND** `BuildMutationScope` MUST build only the projects required by mutation profiles and test host execution
- **AND** it MUST exclude platform integration heads and other non-core projects

### Requirement: Core business mutation profiles are explicit
Mutation execution SHALL run explicit profiles for core business projects.

#### Scenario: Mutation profiles are executed
- **WHEN** `MutationTest` executes
- **THEN** it MUST run profile configs for `Core`, `Runtime`, and `AI`
- **AND** each profile MUST write reports to a dedicated subdirectory under `artifacts/mutation-report/`

### Requirement: Stryker configs enforce core-only scope boundaries
Stryker configuration SHALL be normalized into explicit profile configs with `mutate` filters that target core code paths.

#### Scenario: Reviewing profile configuration
- **WHEN** a profile config is inspected
- **THEN** it MUST include an explicit `project`
- **AND** it MUST include explicit `mutate` patterns for intended core code
- **AND** it MUST exclude provider wrappers and non-core platform/integration heads from mutation scope

### Requirement: Governance tests prevent orchestration drift
Unit governance tests SHALL guard mutation workflow orchestration boundaries.

#### Scenario: CI orchestration regresses
- **WHEN** mutation workflow reintroduces `BuildAll` or direct `dotnet stryker` invocation
- **THEN** governance tests MUST fail with actionable diagnostics
