## MODIFIED Requirements

### Requirement: Template SHALL scaffold Desktop, Bridge, and Tests projects
The generated solution SHALL include:
- a Desktop host with Avalonia + WebView shell (including `MainWindow` and `wwwroot`)
- a Bridge project with interop interfaces/implementations
- a Tests project with baseline bridge tests

The Desktop host project SHALL declare explicit host-layer dependency wiring for Avalonia integration instead of relying on core/runtime transitive Avalonia references.

#### Scenario: Hybrid solution contains expected projects
- **WHEN** a project is created from the template
- **THEN** Desktop, Bridge, and Tests projects are generated with expected baseline files

#### Scenario: Desktop host resolves Avalonia integration explicitly
- **WHEN** the generated Desktop project dependencies are inspected
- **THEN** Avalonia-specific integration is referenced through the host-layer package/dependency path explicitly
- **AND** core/runtime package dependencies remain host-framework-neutral
