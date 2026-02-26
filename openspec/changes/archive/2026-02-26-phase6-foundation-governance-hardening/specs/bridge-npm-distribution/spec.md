## ADDED Requirements

### Requirement: Bridge npm distribution governance SHALL validate package-manager parity
`@agibuild/bridge` distribution governance in `CiPublish` MUST validate install/build/consume smoke paths across `npm`, `pnpm`, and `yarn` for governed sample consumption workflows.

#### Scenario: All package-manager smoke paths pass
- **WHEN** `CiPublish` executes bridge distribution governance checks
- **THEN** npm/pnpm/yarn smoke paths complete successfully with equivalent package API usability outcomes

#### Scenario: Package-manager divergence fails governance
- **WHEN** one package manager fails bridge package consumption while others pass
- **THEN** distribution governance fails with manager-specific diagnostics

### Requirement: Bridge npm distribution governance SHALL validate Node LTS compatibility
Bridge distribution governance in `CiPublish` MUST verify `@agibuild/bridge` package consumption against the supported Node LTS compatibility window defined by repository policy.

#### Scenario: Node LTS matrix is satisfied
- **WHEN** `CiPublish` governance runs bridge package smoke checks on all declared LTS versions
- **THEN** each version passes package install/build/typed-import validation

#### Scenario: Unsupported Node LTS regression fails governance
- **WHEN** a declared Node LTS version fails bridge package smoke validation
- **THEN** governance marks the run failed before release publication
