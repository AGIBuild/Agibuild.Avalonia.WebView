# Tasks: Template E2E Testing & Sub-package Publishing

## Task 1: Make sub-packages packable
**Acceptance**: Core, Adapters.Abstractions, Runtime, Bridge.Generator, Testing all have `IsPackable=true` + `PackageId`. `dotnet pack` produces individual .nupkg files.

## Task 2: Create template pack project
**Acceptance**: `templates/Agibuild.Avalonia.WebView.Templates.csproj` with `PackageType=Template`. `dotnet pack` produces `Agibuild.Avalonia.WebView.Templates.*.nupkg`.

## Task 3: Add Nuke PackAll target
**Acceptance**: `./build.sh PackAll` packs all 5 sub-packages to `artifacts/packages/`.

## Task 4: Add Nuke PackTemplate + PublishTemplate targets
**Acceptance**: `./build.sh PackTemplate` packs template. `./build.sh PublishTemplate` pushes to NuGet.

## Task 5: Add Nuke TemplateE2E target
**Acceptance**: `./build.sh TemplateE2E` runs full cycle: pack → install → create → inject → build → test → cleanup. All 11 tests pass (3 original + 8 injected).

## Task 6: Verify
**Acceptance**: `./build.sh TemplateE2E` passes end-to-end. Existing 525 unit tests pass with zero regressions.
