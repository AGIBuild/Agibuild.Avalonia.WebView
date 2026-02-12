# Spec: Template E2E Testing & Sub-package Publishing

Sub-packages (Core, Adapters.Abstractions, Runtime, Bridge.Generator, Testing) are independently packable. Template is packable as NuGet for `dotnet new install`. Nuke targets orchestrate packaging, publishing, and E2E verification.

## Requirements

### TE-1: Sub-package NuGet packaging
Core, Adapters.Abstractions, Runtime, Bridge.Generator, and Testing produce individual .nupkg files via `dotnet pack`.

### TE-2: Template NuGet packaging
`Agibuild.Avalonia.WebView.Templates.nupkg` packages the `agibuild-hybrid` template. Users install via `dotnet new install Agibuild.Avalonia.WebView.Templates`.

### TE-3: Nuke `PackAll` target
Packs all 5 sub-packages to `artifacts/packages/`.

### TE-4: Nuke `PackTemplate` / `PublishTemplate` targets
Pack and publish the template NuGet package.

### TE-5: Nuke `TemplateE2E` target
End-to-end cycle: pack all packages → install template → create project → write nuget.config → patch prerelease versions → inject Bridge E2E tests → build → test (11 pass) → cleanup.

## Test Coverage
- 8 injected E2E tests (JsExport attributes, implementation, MockBridge Expose/GetProxy, coexistence, source generator)
- 3 template-original tests (Greet implementation, MockBridge Expose, proxy setup)
- 525 existing unit tests: zero regressions
