## MODIFIED Requirements

### Requirement: Template SHALL scaffold Desktop, Bridge, and Tests projects
The generated solution SHALL include:
- a Desktop host with Avalonia + WebView shell (including `MainWindow` and `wwwroot`)
- a Bridge project with interop interfaces/implementations
- a Tests project with baseline bridge tests

The Desktop host project SHALL declare explicit host-layer dependency wiring for Avalonia integration by referencing `Agibuild.Fulora.Avalonia` directly and SHALL NOT reference legacy package identity `Agibuild.Fulora`.

#### Scenario: Hybrid solution contains expected projects
- **WHEN** a project is created from the template
- **THEN** Desktop, Bridge, and Tests projects are generated with expected baseline files

#### Scenario: Desktop host resolves Avalonia integration explicitly
- **WHEN** the generated Desktop project dependencies are inspected
- **THEN** Avalonia-specific integration is referenced through `Agibuild.Fulora.Avalonia`
- **AND** core/runtime package dependencies remain host-framework-neutral
- **AND** legacy package identity `Agibuild.Fulora` is absent from generated Desktop dependency declarations
