## ADDED Requirements

### Requirement: WindowsBase conflict warnings are eliminated in governed builds
Governed build targets MUST NOT emit `MSB3277` warnings whose message indicates `WindowsBase` version conflict.
The pipeline SHALL treat this warning pattern as a build-graph defect, not as accepted baseline noise.

#### Scenario: Non-Windows CI build completes without WindowsBase conflict warnings
- **WHEN** warning governance scans outputs from governed build targets on macOS/Linux CI
- **THEN** no `MSB3277` warning containing `WindowsBase` is present in the classification input

#### Scenario: Conflict warning reappears
- **WHEN** a governed build emits `MSB3277` with `WindowsBase` conflict text
- **THEN** warning governance classifies it as `new-regression` and fails the quality gate

### Requirement: Windows dependency boundaries are explicit and host-safe
Project and package references related to the Windows adapter MUST be structured so non-Windows hosts do not resolve Windows-only assembly conflict paths while preserving Windows runtime package correctness.

#### Scenario: Packaging references remain valid for Windows consumers
- **WHEN** the package is restored and used on Windows with the Windows adapter enabled
- **THEN** required WebView2 runtime assemblies remain resolvable without adding manual consumer-side fixes

#### Scenario: Cross-host restore/build remains deterministic
- **WHEN** the same governed targets run on Windows and non-Windows hosts
- **THEN** warning classification output is host-consistent for this conflict class (zero accepted baseline entries)

### Requirement: WebView2 reference model supports host-agnostic pack
The build system MUST allow any supported host OS to build and pack all platform package artifacts without importing WebView2 WPF/WinForms compile references through package targets.

#### Scenario: Package targets are not auto-imported for WebView2 in affected projects
- **WHEN** affected projects evaluate package assets for `Microsoft.Web.WebView2`
- **THEN** `build` and `buildTransitive` target injection is disabled for those projects

#### Scenario: Windows adapter compile still succeeds with explicit core reference
- **WHEN** `Agibuild.Fulora.Adapters.Windows` is built on any host
- **THEN** compile-time WebView2 API binding resolves through explicit `Microsoft.Web.WebView2.Core` reference without requiring WPF/WinForms assemblies
