## MODIFIED Requirements

### Requirement: Template SHALL scaffold Desktop, Bridge, and Tests projects

The generated solution SHALL include:
- a Desktop host with Avalonia + WebView shell (including `wwwroot`)
- a Bridge project with interop interfaces/implementations
- a Tests project with baseline bridge tests
- a Web frontend project with `@agibuild/bridge` npm package, typed service layer, and bridge readiness detection

The Desktop host project SHALL declare explicit host-layer dependency wiring for Avalonia integration by referencing `Agibuild.Fulora.Avalonia` directly and SHALL NOT reference legacy package identity `Agibuild.Fulora`.

#### Scenario: Hybrid solution contains expected projects

- **WHEN** a project is created from the template
- **THEN** Desktop, Bridge, Tests, and Web projects are generated with expected baseline files

#### Scenario: Desktop host resolves Avalonia integration explicitly

- **WHEN** the generated Desktop project dependencies are inspected
- **THEN** Avalonia-specific integration is referenced through `Agibuild.Fulora.Avalonia`
- **AND** core/runtime package dependencies remain host-framework-neutral
- **AND** legacy package identity `Agibuild.Fulora` is absent from generated Desktop dependency declarations

#### Scenario: Web project includes @agibuild/bridge dependency

- **WHEN** the generated Web project `package.json` is inspected
- **THEN** `@agibuild/bridge` SHALL be listed as a dependency with a compatible version

#### Scenario: Web project includes typed service layer

- **WHEN** the generated Web project source is inspected
- **THEN** a `bridge/services.ts` file SHALL exist with typed service proxy exports using `bridgeClient.getService<T>()`

#### Scenario: Web project includes bridge readiness detection

- **WHEN** the generated React Web project source is inspected
- **THEN** a `hooks/useBridge.ts` file SHALL exist with a `useBridgeReady()` hook

### Requirement: Template SHALL integrate bridge.d.ts generation

The template SHALL configure MSBuild to generate `bridge.d.ts` from the Bridge project's `[JsExport]`/`[JsImport]` interfaces into the Web project's `src/bridge/` directory.

#### Scenario: bridge.d.ts is generated on build

- **WHEN** the generated solution is built via `dotnet build`
- **THEN** `bridge.d.ts` SHALL be written to the Web project's `src/bridge/` directory

#### Scenario: tsconfig.json includes bridge.d.ts

- **WHEN** the generated Web project `tsconfig.json` is inspected
- **THEN** the `include` array SHALL contain the `bridge.d.ts` file path

### Requirement: Template SHALL include development-mode bridge middleware

The template Web project SHALL configure bridge middleware for development mode.

#### Scenario: Development mode enables logging middleware

- **WHEN** the generated Web project runs in development mode
- **THEN** bridge calls SHALL be logged via `withLogging()` middleware

#### Scenario: Production mode does not include logging middleware

- **WHEN** the generated Web project runs in production mode
- **THEN** logging middleware SHALL NOT be active
