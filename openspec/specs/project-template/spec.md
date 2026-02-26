## Purpose
Define requirements for the `dotnet new agibuild-hybrid` template that scaffolds Avalonia + WebView hybrid applications with Desktop, Bridge, and Tests projects.
## Requirements
### Requirement: Template metadata SHALL be well-defined
The template SHALL define stable identity and classification metadata in `template.json` so it can be discovered and invoked consistently, including explicit shell preset metadata.

#### Scenario: Template identity and short name are present
- **WHEN** template metadata is inspected
- **THEN** identity is `Agibuild.Avalonia.WebView.HybridTemplate` and short name is `agibuild-hybrid`

#### Scenario: Template classifications and directory preference are configured
- **WHEN** template metadata is inspected
- **THEN** classifications include Desktop, Mobile, Hybrid, Avalonia, WebView and `PreferNameDirectory` is enabled

#### Scenario: Shell preset symbol metadata is defined
- **WHEN** template metadata is inspected
- **THEN** shell preset symbol is present with explicit choices and default value

### Requirement: Template SHALL scaffold Desktop, Bridge, and Tests projects
The generated solution SHALL include:
- a Desktop host with Avalonia + WebView shell (including `MainWindow` and `wwwroot`)
- a Bridge project with interop interfaces/implementations
- a Tests project with baseline bridge tests

#### Scenario: Hybrid solution contains expected projects
- **WHEN** a project is created from the template
- **THEN** Desktop, Bridge, and Tests projects are generated with expected baseline files

### Requirement: Template SHALL support framework selection
The template SHALL expose framework and shell preset choice parameters with supported values and generate conditional source content based on selected options.

#### Scenario: Framework choice drives scaffolded content
- **WHEN** the framework parameter is set to one of `vanilla`, `react`, or `vue`
- **THEN** generated source files match the selected framework

#### Scenario: Shell preset choice drives desktop host wiring
- **WHEN** shell preset parameter is set to `baseline` or `app-shell`
- **THEN** generated desktop source contains the corresponding preset-specific shell wiring path

### Requirement: Hybrid template test project SHALL emit xUnit v3 dependencies
The `dotnet new agibuild-hybrid` template output for `HybridApp.Tests` SHALL reference xUnit v3-compatible packages so newly scaffolded projects do not start from deprecated xUnit v2 baselines.

#### Scenario: Generated test project uses xUnit v3
- **WHEN** a project is scaffolded from the hybrid template
- **THEN** `HybridApp.Tests.csproj` references `xunit.v3` and a compatible test runner package for `dotnet test`

### Requirement: Template debug startup path SHALL be deterministic
Hybrid project template SHALL include deterministic web-debug startup conventions for local development.

#### Scenario: Debug startup contract is present in template
- **WHEN** template artifacts are generated
- **THEN** web startup configuration includes deterministic host URL/port handshake and bridge-ready script wiring

### Requirement: Template SHALL preserve bridge typing path
Template-generated web project SHALL compile with generated bridge declaration contracts.

#### Scenario: Template web TypeScript compile succeeds
- **WHEN** template web project runs TypeScript compile validation
- **THEN** bridge type imports resolve and compile without type errors

### Requirement: Framework selection SHALL materialize framework-specific web scaffold
Template framework selection SHALL generate concrete framework-specific web scaffold content for `react` and `vue` choices.

#### Scenario: React framework emits React Vite scaffold
- **WHEN** template is instantiated with `--framework react`
- **THEN** generated project contains React Vite web scaffold with buildable `package.json` scripts

#### Scenario: Vue framework emits Vue Vite scaffold
- **WHEN** template is instantiated with `--framework vue`
- **THEN** generated project contains Vue Vite web scaffold with buildable `package.json` scripts

