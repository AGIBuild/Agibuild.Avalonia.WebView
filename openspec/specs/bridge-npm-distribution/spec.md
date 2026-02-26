## Purpose
Define contracts for distributing a typed bridge runtime npm package (`@agibuild/bridge`).
## Requirements
### Requirement: Repository SHALL include source package for `@agibuild/bridge`
The repository SHALL contain a buildable npm package source for bridge runtime consumption.

#### Scenario: Package workspace is present
- **WHEN** contributors inspect the repository
- **THEN** `packages/bridge/package.json`, `tsconfig.json`, and `src/index.ts` exist with deterministic exports

### Requirement: Bridge npm package SHALL expose typed runtime client APIs
`@agibuild/bridge` package SHALL export typed APIs for readiness and method invocation flows.

#### Scenario: Consumer imports package APIs
- **WHEN** a TypeScript consumer imports from `@agibuild/bridge`
- **THEN** exported types and runtime symbols compile without additional runtime dependencies

### Requirement: Bridge npm client SHALL expose typed service contract semantics
`@agibuild/bridge` SHALL provide a typed service client contract that supports deterministic method invocation using zero-argument or single-object-argument semantics.

#### Scenario: Service method with non-object parameter is rejected
- **WHEN** a typed service proxy method is invoked with a non-object parameter
- **THEN** bridge client throws deterministic validation error instead of implicit parameter coercion

