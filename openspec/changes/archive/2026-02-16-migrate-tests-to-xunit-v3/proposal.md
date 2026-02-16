## Why

The current test projects still depend on xUnit v2 packages, which are aging and create version drift across unit, integration automation, sample, and template test projects. Upgrading to xUnit v3 improves long-term maintainability and keeps the test toolchain aligned with the repository's Phase 3 quality goals.

## What Changes

- Migrate scoped repository test projects from `xunit` v2 to `xunit.v3`, including the Avalonia automation test project via a local compatibility shim.
- Keep `xunit.runner.visualstudio` on a v3-compatible version and validate `dotnet test` execution.
- Update template test project dependencies so newly scaffolded projects use xUnit v3 by default.
- Add/adjust CI/build validation to ensure test package migration does not regress lane automation.

## Capabilities

### New Capabilities
- `<none>`

### Modified Capabilities
- `webview-testing-harness`: Require xUnit v3 package baseline for scoped repository test projects, including Avalonia headless automation.
- `project-template`: Require generated `HybridApp.Tests` to reference xUnit v3-compatible packages.

## Impact

- Affected code: test project `.csproj` files under `tests/`, `samples/avalonia-react/`, and `templates/agibuild-hybrid/`.
- Affected build/test flow: `dotnet test` and Nuke test-related targets.
- Dependency impact: replace `xunit` v2 with `xunit.v3` in scoped projects; keep VSTest adapter compatibility via `xunit.runner.visualstudio`; use local Avalonia headless compatibility wiring in automation tests.
- Goal/roadmap alignment: supports **G4 (Contract-Driven Testability)** and **Phase 3 / 3.3 (Performance & Quality)**, and keeps **Phase 3 / 3.1 (Project Template)** current.

## Non-goals

- No changes to WebView runtime contracts or adapter behavior.
- No expansion of test feature scope beyond package/toolchain migration.
- No refactor of existing test logic unrelated to xUnit package compatibility.
