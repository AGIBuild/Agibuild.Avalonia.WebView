## ADDED Requirements

### Requirement: Hybrid template test project SHALL emit xUnit v3 dependencies
The `dotnet new agibuild-hybrid` template output for `HybridApp.Tests` SHALL reference xUnit v3-compatible packages so newly scaffolded projects do not start from deprecated xUnit v2 baselines.

#### Scenario: Generated test project uses xUnit v3
- **WHEN** a project is scaffolded from the hybrid template
- **THEN** `HybridApp.Tests.csproj` references `xunit.v3` and a compatible test runner package for `dotnet test`
