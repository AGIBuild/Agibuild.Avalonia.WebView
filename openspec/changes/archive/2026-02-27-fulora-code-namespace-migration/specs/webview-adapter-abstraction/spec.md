## MODIFIED Requirements

### Requirement: Adapter abstractions assembly
The system SHALL provide an assembly named `Agibuild.Fulora.Adapters.Abstractions` targeting `net10.0`.
This assembly SHALL reference `Agibuild.Fulora.Core` and SHALL NOT reference any platform-specific adapter projects.

#### Scenario: Abstractions are platform-free
- **WHEN** a project references `Agibuild.Fulora.Adapters.Abstractions`
- **THEN** it builds without any platform-specific adapter dependencies
