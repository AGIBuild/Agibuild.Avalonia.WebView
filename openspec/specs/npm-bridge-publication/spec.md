## Purpose
Define requirements for automated npm publication of `@agibuild/bridge` package to the public npm registry.

## Requirements

### Requirement: Package metadata is complete for public npm registry
The `@agibuild/bridge` package.json SHALL contain all metadata fields required for discoverable public npm publication.

#### Scenario: Package metadata is valid for publication
- **WHEN** a contributor inspects `packages/bridge/package.json`
- **THEN** it SHALL contain `repository`, `homepage`, `keywords`, `author`, and `bugs` fields with correct values

### Requirement: npm publish is automated via nuke build target
A nuke build target SHALL exist that executes `npm publish` for the bridge package with token-based authentication.

#### Scenario: NpmPublish target executes successfully
- **WHEN** `nuke NpmPublish` is invoked with a valid `NPM_TOKEN` environment variable
- **THEN** the target SHALL run `npm publish --access public` from `packages/bridge/`

#### Scenario: NpmPublish target fails without token
- **WHEN** `nuke NpmPublish` is invoked without `NPM_TOKEN`
- **THEN** the target SHALL fail with a clear error message before attempting publish

### Requirement: npm and NuGet publish are independently gated
The npm publish target SHALL be independent from the NuGet publish target so that a failure in one does not block the other.

#### Scenario: NuGet publish failure does not block npm
- **WHEN** `nuke Publish` fails (NuGet)
- **THEN** `nuke NpmPublish` can still be invoked independently
