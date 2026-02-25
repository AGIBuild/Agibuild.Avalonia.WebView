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
