## MODIFIED Requirements

### Requirement: Distribution channel completeness gate
The release distribution determinism contract SHALL treat npm publication as a required distribution channel alongside NuGet for stable releases.

#### Scenario: Stable release requires both NuGet and npm success
- **WHEN** a stable version tag (no pre-release suffix) triggers the release workflow
- **THEN** both the NuGet publish job and the npm publish job MUST succeed for the release to be considered complete

#### Scenario: Pre-release allows npm publish failure as warning
- **WHEN** a pre-release version tag triggers the release workflow
- **THEN** npm publish failure SHALL be logged as a warning but SHALL NOT block the NuGet publish or GitHub Release creation
