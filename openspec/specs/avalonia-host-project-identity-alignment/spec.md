## Purpose
Define canonical filename identity requirements for the Avalonia host project so project file naming remains deterministic and aligned with package identity.

## Requirements

### Requirement: Avalonia host project filename SHALL match canonical package identity
The repository SHALL name the Avalonia host project file using the canonical package identity token `Agibuild.Fulora.Avalonia` so project-level identity and package-level identity remain deterministic.

#### Scenario: Canonical project filename is used
- **WHEN** the Avalonia host project is referenced in repository sources
- **THEN** the project file name is `Agibuild.Fulora.Avalonia.csproj`

### Requirement: Governed repository references MUST use canonical host project filename
All governed references to the Avalonia host project path MUST use `Agibuild.Fulora.Avalonia.csproj`; legacy filename references MUST NOT remain in repository-controlled build/test/template assets.

#### Scenario: Legacy host project filename appears in governed assets
- **WHEN** a governed file references `src/Agibuild.Fulora/Agibuild.Fulora.csproj`
- **THEN** validation fails until the reference is updated to the canonical filename
