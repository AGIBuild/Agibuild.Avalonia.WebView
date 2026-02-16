## Purpose
Define requirements for the `dotnet new agibuild-hybrid` template that scaffolds Avalonia + WebView hybrid applications with Desktop, Bridge, and Tests projects.

## Requirements

### Requirement: Template metadata SHALL be well-defined
The template SHALL define stable identity and classification metadata in `template.json` so it can be discovered and invoked consistently.

#### Scenario: Template identity and short name are present
- **WHEN** template metadata is inspected
- **THEN** identity is `Agibuild.Avalonia.WebView.HybridTemplate` and short name is `agibuild-hybrid`

#### Scenario: Template classifications and directory preference are configured
- **WHEN** template metadata is inspected
- **THEN** classifications include Desktop, Mobile, Hybrid, Avalonia, WebView and `PreferNameDirectory` is enabled

### Requirement: Template SHALL scaffold Desktop, Bridge, and Tests projects
The generated solution SHALL include:
- a Desktop host with Avalonia + WebView shell (including `MainWindow` and `wwwroot`)
- a Bridge project with interop interfaces/implementations
- a Tests project with baseline bridge tests

#### Scenario: Hybrid solution contains expected projects
- **WHEN** a project is created from the template
- **THEN** Desktop, Bridge, and Tests projects are generated with expected baseline files

### Requirement: Template SHALL support framework selection
The template SHALL expose a framework choice parameter with supported values and generate conditional source content based on the selected framework.

#### Scenario: Framework choice drives scaffolded content
- **WHEN** the framework parameter is set to one of `vanilla`, `react`, or `vue`
- **THEN** generated source files match the selected framework

### Requirement: Hybrid template test project SHALL emit xUnit v3 dependencies
The `dotnet new agibuild-hybrid` template output for `HybridApp.Tests` SHALL reference xUnit v3-compatible packages so newly scaffolded projects do not start from deprecated xUnit v2 baselines.

#### Scenario: Generated test project uses xUnit v3
- **WHEN** a project is scaffolded from the hybrid template
- **THEN** `HybridApp.Tests.csproj` references `xunit.v3` and a compatible test runner package for `dotnet test`
