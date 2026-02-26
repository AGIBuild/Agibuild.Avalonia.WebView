## MODIFIED Requirements

### Requirement: Core contracts assembly
The system SHALL provide a public contracts assembly named `Agibuild.Fulora.Core` with root namespace `Agibuild.Fulora`.
The assembly SHALL target `net10.0` and SHALL NOT reference any platform adapter projects.

#### Scenario: Core is platform-agnostic
- **WHEN** a project references `Agibuild.Fulora.Core` only
- **THEN** it builds without any platform-specific adapter dependencies
