# Proposal: Template E2E Testing & Sub-package Publishing

## Problem
The `agibuild-hybrid` project template references three separate NuGet packages (Core, Bridge.Generator, main WebView) but only the main package was individually packable. There was no automated end-to-end verification that a project created from the template actually compiles and passes tests. Template packaging for NuGet distribution was also missing.

## Solution
1. Make Core, Adapters.Abstractions, Runtime, Bridge.Generator, and Testing independently packable with proper `IsPackable` + `PackageId` configuration.
2. Create a template pack project (`Agibuild.Avalonia.WebView.Templates.csproj`) for `dotnet new install` via NuGet.
3. Add four Nuke build targets:
   - `PackAll`: Packs sub-packages (Core, Abstractions, Runtime, Generator, Testing)
   - `PackTemplate`: Packs the template as a NuGet package
   - `PublishTemplate`: Pushes template .nupkg to NuGet
   - `TemplateE2E`: Full end-to-end test cycle (pack → install → create → inject tests → build → test → cleanup)

## Alternatives Considered
- **Standalone xUnit project for E2E**: Rejected in favor of Nuke target, consistent with existing `NugetPackageTest` pattern.
- **ProjectReference patching**: Rejected. Uses local NuGet feed instead, keeping template-generated code unmodified.
- **Internal type testing (MockWebViewAdapter)**: Rejected due to `internal` access restrictions. Uses public `MockBridgeService` API instead.
