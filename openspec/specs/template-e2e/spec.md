## Purpose
Define template E2E and packaging governance for sub-packages and template distribution.
## Requirements
### Requirement: Sub-packages are independently packable
Core, Adapters.Abstractions, Runtime, Bridge.Generator, and Testing packages SHALL be individually packable via `dotnet pack`.

#### Scenario: Sub-package pack artifacts are produced
- **WHEN** packaging automation runs for sub-packages
- **THEN** individual `.nupkg` artifacts are generated for each sub-package

### Requirement: Template package is distributable via dotnet new
The template package SHALL produce `Agibuild.Avalonia.WebView.Templates.nupkg` and SHALL be installable through `dotnet new install`.

#### Scenario: Template package installs successfully
- **WHEN** the generated template package is installed with `dotnet new install`
- **THEN** the `agibuild-hybrid` template becomes available for project scaffolding

### Requirement: Nuke packaging targets orchestrate deterministic outputs
Nuke targets SHALL provide deterministic `PackAll`, `PackTemplate`, and `PublishTemplate` workflows for sub-package and template artifacts.

#### Scenario: PackAll produces aggregated package output
- **WHEN** `PackAll` target executes
- **THEN** package artifacts are generated under repository-defined artifact paths

### Requirement: TemplateE2E validates full packaging-to-test workflow
The `TemplateE2E` workflow SHALL cover end-to-end template verification from package generation through scaffold/build/test/cleanup.

#### Scenario: TemplateE2E completes with passing verification tests
- **WHEN** TemplateE2E automation executes
- **THEN** scaffolded project build and tests pass and cleanup completes deterministically

### Requirement: TemplateE2E SHALL validate framework-specific web build paths
TemplateE2E workflow SHALL validate generated React and Vue web scaffold build paths in addition to baseline .NET build/test flow.

#### Scenario: React scaffold web build succeeds in TemplateE2E
- **WHEN** TemplateE2E runs against react framework template output
- **THEN** `npm install` and `npm run build` succeed for generated React web project

#### Scenario: Vue scaffold web build succeeds in TemplateE2E
- **WHEN** TemplateE2E runs against vue framework template output
- **THEN** `npm install` and `npm run build` succeed for generated Vue web project

