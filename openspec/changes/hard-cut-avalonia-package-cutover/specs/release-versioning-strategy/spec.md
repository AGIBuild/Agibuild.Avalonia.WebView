## MODIFIED Requirements

### Requirement: Stable package metadata SHALL meet publication standards
Stable (non-prerelease) NuGet packages SHALL include valid license expression, project URL, and description without "preview" language.
Stable release package identities SHALL use the canonical `Agibuild.Fulora.` prefix for primary distributable artifacts, and the primary Avalonia host package identity SHALL be `Agibuild.Fulora.Avalonia`.

#### Scenario: Stable package nuspec is valid
- **WHEN** `nuke ValidatePackage` runs for a stable-versioned package
- **THEN** nuspec contains non-empty `licenseUrl` or `license`, valid `projectUrl`, and description without "preview" or "pre-release" wording

#### Scenario: Stable package identity is canonical
- **WHEN** release validation inspects primary stable package outputs
- **THEN** package IDs use the `Agibuild.Fulora.` prefix as canonical identity
- **AND** the primary host package ID equals `Agibuild.Fulora.Avalonia`
- **AND** legacy primary host package ID `Agibuild.Fulora` is not accepted
