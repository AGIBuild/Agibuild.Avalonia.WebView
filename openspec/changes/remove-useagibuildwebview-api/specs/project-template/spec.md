## MODIFIED Requirements

### Requirement: Template SHALL scaffold Desktop, Bridge, and Tests projects
The generated solution SHALL include:
- a Desktop host with Avalonia + WebView shell (including `wwwroot`)
- a Bridge project with interop interfaces/implementations
- a Tests project with baseline bridge tests
- a Web frontend project with `@agibuild/bridge`, generated bridge artifacts, and profile-based bridge startup wiring

The Desktop host project SHALL declare explicit host-layer dependency wiring for Avalonia integration by referencing `Agibuild.Fulora.Avalonia` directly and SHALL NOT reference legacy package identity `Agibuild.Fulora`.
The generated Desktop startup entrypoint SHALL initialize Fulora through `UseFulora()` and SHALL NOT use legacy bootstrap alias `UseAgibuildWebView()`.

#### Scenario: Hybrid solution contains expected projects
- **WHEN** a project is created from the template
- **THEN** Desktop, Bridge, Tests, and Web projects are generated with expected baseline files

#### Scenario: Desktop host resolves Avalonia integration explicitly
- **WHEN** the generated Desktop project dependencies are inspected
- **THEN** Avalonia-specific integration is referenced through `Agibuild.Fulora.Avalonia`
- **AND** core/runtime package dependencies remain host-framework-neutral
- **AND** legacy package identity `Agibuild.Fulora` is absent from generated Desktop dependency declarations

#### Scenario: Desktop host uses canonical Fulora bootstrap entrypoint
- **WHEN** the generated Desktop startup code is inspected
- **THEN** the host initialization path calls `UseFulora()`
- **AND** `UseAgibuildWebView()` does not appear in template-generated startup code

#### Scenario: Web project includes @agibuild/bridge dependency
- **WHEN** the generated Web project `package.json` is inspected
- **THEN** `@agibuild/bridge` SHALL be listed as a dependency with a compatible version

#### Scenario: Web project uses generated service contracts
- **WHEN** the generated Web project source is inspected
- **THEN** bridge service contracts and DTO types SHALL be consumed from generated artifacts by default

#### Scenario: Web project uses profile-based readiness wiring
- **WHEN** the generated React or Vue project source is inspected
- **THEN** bridge readiness SHALL be wired through profile entry API instead of ad-hoc app-layer polling loops
