## MODIFIED Requirements

### Requirement: Stable package metadata SHALL meet publication standards
Stable (non-prerelease) NuGet packages SHALL include valid license expression, project URL, and description without "preview" language.
Stable release package identities SHALL use the canonical `Agibuild.Fulora.` prefix for primary distributable artifacts.

#### Scenario: Stable package nuspec is valid
- **WHEN** `nuke ValidatePackage` runs for a stable-versioned package
- **THEN** nuspec contains non-empty `licenseUrl` or `license`, valid `projectUrl`, and description without "preview" or "pre-release" wording

#### Scenario: Stable package identity is canonical
- **WHEN** release validation inspects primary stable package outputs
- **THEN** package IDs use the `Agibuild.Fulora.` prefix as canonical identity
