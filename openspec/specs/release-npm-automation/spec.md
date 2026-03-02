## ADDED Requirements

### Requirement: npm publish job in release workflow
The release workflow SHALL include an `npm-publish` job that publishes `@agibuild/bridge` to the npm registry when triggered by a version tag push.

#### Scenario: Tag push triggers npm publish
- **WHEN** a git tag matching `v*` is pushed to the repository
- **THEN** the release workflow SHALL execute an `npm-publish` job after the build job succeeds

#### Scenario: npm publish uses token authentication
- **WHEN** the `npm-publish` job executes
- **THEN** it SHALL authenticate using the `NPM_TOKEN` secret from the `npm` GitHub Actions environment

#### Scenario: npm publish builds before publishing
- **WHEN** the `npm-publish` job executes
- **THEN** it SHALL run `npm ci` and `npm run build` in `packages/bridge/` before running `npm publish`

#### Scenario: npm publish skips on duplicate version
- **WHEN** the npm registry already contains `@agibuild/bridge` at the tag version
- **THEN** the publish step SHALL NOT fail (idempotent behavior)

### Requirement: npm package version sync with git tag
The `@agibuild/bridge` package version SHALL be synchronized with the git tag version during the release workflow.

#### Scenario: Stable tag produces stable npm version
- **WHEN** the git tag is `v1.0.0` (no pre-release suffix)
- **THEN** the npm publish step SHALL set the package version to `1.0.0` before publishing

#### Scenario: Pre-release tag produces pre-release npm version
- **WHEN** the git tag contains a pre-release suffix (e.g., `v1.1.0-preview`)
- **THEN** the npm publish step SHALL set the package version to `1.1.0-preview` and publish with `--tag preview`

### Requirement: Release workflow npm environment configuration
The repository SHALL document the required `NPM_TOKEN` secret configuration for the npm publish job.

#### Scenario: Missing NPM_TOKEN blocks publish
- **WHEN** the `NPM_TOKEN` secret is not configured in the `npm` environment
- **THEN** the `npm-publish` job SHALL fail with a clear error indicating missing credentials
