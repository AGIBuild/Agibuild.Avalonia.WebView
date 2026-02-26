## Verification Summary

### Commands

1. `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj`
   - Result: Initial run passed (`719/719`) before template generation validation hardening.

2. Template generation/build validation (round 1):
   - `dotnet new install d:\src\projects\WebView\templates`
   - `dotnet new agibuild-hybrid --shellPreset app-shell`
   - `dotnet new agibuild-hybrid --shellPreset baseline`
   - `dotnet build <ShellApp>.sln`
   - `dotnet build <BaseApp>.sln`
   - `dotnet test <ShellApp.Tests>.csproj --no-build`
   - `dotnet test <BaseApp.Tests>.csproj --no-build`
   - Result: Failed on shell namespace availability and `WithInterFont()` compatibility.

3. Template generation/build validation (round 2, different fix strategy):
   - Added runtime package reference and removed `WithInterFont()`.
   - Re-ran same generation/build/test command set.
   - Result: Baseline succeeded; app-shell still failed due shell namespace mismatch against currently published package.

4. Template generation/build validation (round 3, design adjustment):
   - Reworked app-shell preset to compile-safe event-based shell wiring using public package APIs.
   - Re-ran same generation/build/test command set.
   - Result: Passed for both app-shell and baseline (`build + test` success).

5. `dotnet test tests/Agibuild.Fulora.UnitTests/Agibuild.Fulora.UnitTests.csproj`
   - Result: One flaky dispatcher test timeout on first rerun.
   - Retry round 2: targeted test filter passed (`1/1`).
   - Retry round 3: full unit suite passed (`719/719`).

6. `dotnet test tests/Agibuild.Fulora.Integration.Tests.Automation/Agibuild.Fulora.Integration.Tests.Automation.csproj`
   - Result: Passed (`132/132`).

## Retry Log

### A) Template generation validation failures

- **Round 1**
  - Failure cause: app-shell preset referenced shell namespace not available in currently resolved package; baseline also failed due `WithInterFont()` method mismatch.
  - Change applied: add runtime package reference + remove `.WithInterFont()`.
  - Result: partial improvement (baseline fixed, app-shell still failed).

- **Round 2**
  - Failure cause: shell namespace still unavailable in generated app-shell against currently resolved package.
  - Change applied: redesign app-shell preset implementation to event-based shell governance using public core APIs (`NewWindowRequested`, `PermissionRequested`, `DownloadRequested`) instead of direct shell runtime type dependency.
  - Result: app-shell and baseline both compile and test successfully.

- **Round 3**
  - Validation rerun after redesign.
  - Result: full success for template generation/build/test matrix.

### B) Unit test flaky timeout

- **Round 1**
  - Failure cause: `ContractSemanticsV1DispatcherMarshalingTests.TryGetWebViewHandle_off_thread_dispatches_to_ui_thread` timed out.
  - Change applied: no code change; treated as transient lane flake.
  - Result: failure persisted in that run.

- **Round 2**
  - Action: isolated targeted retry with test filter.
  - Result: pass (`1/1`).

- **Round 3**
  - Action: full unit suite rerun.
  - Result: pass (`719/719`).

## Requirements Traceability

### `template-shell-presets` (new capability)

- Explicit preset choices
  - `Hybrid_template_metadata_exposes_shell_preset_choices` (CT)

- App-shell preset wiring emitted
  - `Hybrid_template_source_contains_shell_preset_wiring_markers` (CT)
  - Template generation/build validation round 3 (IT-like command verification)

- Baseline preset remains minimal
  - Template generation/build validation round 3 with `--shellPreset baseline` (IT-like command verification)

### `project-template` (modified capability)

- Metadata includes shell preset symbol + default/choices
  - `Hybrid_template_metadata_exposes_shell_preset_choices` (CT)

- Framework + shell preset coexistence and source generation
  - `Hybrid_template_source_contains_shell_preset_wiring_markers` (CT)
  - Template generation/build validation round 3 (app-shell + baseline) (IT-like command verification)

### TemplateE2E build path validation (implementation evidence)

- Build target wiring includes app-shell preset parameter
  - `Build_pipeline_exposes_lane_targets_and_machine_readable_reports` asserts `--shellPreset app-shell` marker in `build/Build.cs` (CT governance)
