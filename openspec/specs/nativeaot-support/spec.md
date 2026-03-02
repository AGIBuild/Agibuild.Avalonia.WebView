## Purpose

Define NativeAOT publish support validation and documentation for Agibuild.Fulora apps, ensuring source generator approach works with trimming and AOT compilation.

## ADDED Requirements

### Requirement: NativeAOT publish succeeds for at least one sample
At least one Fulora sample SHALL publish successfully with `PublishAot=true` and trimming enabled on at least one target platform.

#### Scenario: AOT publish produces executable
- **WHEN** a developer runs `dotnet publish -p:PublishAot=true` on the validated sample for the validated platform
- **THEN** the publish SHALL complete without trimmer or AOT errors
- **AND** the output SHALL include a native executable

#### Scenario: AOT-published app runs and bridge works
- **WHEN** the AOT-published executable is run
- **THEN** the app SHALL launch, load the WebView, and at least one bridge call SHALL succeed

### Requirement: Source generator compatibility with trimming
Bridge source generators SHALL be compatible with IL trimming such that generated code is not incorrectly removed.

#### Scenario: Trimmer preserves bridge types
- **WHEN** publish runs with trimming enabled
- **THEN** `[JsExport]` and `[JsImport]` types and their dependencies SHALL be preserved (or explicitly annotated for preservation)

#### Scenario: Required annotations are documented
- **WHEN** `[DynamicDependency]` or `[RequiresUnreferencedCode]` are required for AOT compatibility
- **THEN** such requirements SHALL be documented with examples

### Requirement: CI validation target
CI SHALL include a validation target that runs NativeAOT publish on the validated sample.

#### Scenario: CI runs AOT publish
- **WHEN** CI runs (on main or PR)
- **THEN** a job or target SHALL execute `dotnet publish -p:PublishAot=true` on the validated sample
- **AND** the job SHALL fail if publish fails

#### Scenario: AOT validation is opt-in or gated
- **WHEN** AOT validation is resource-intensive
- **THEN** it MAY be gated (e.g., scheduled, or on-demand) rather than blocking every PR

### Requirement: NativeAOT documentation
Documentation SHALL describe how to enable NativeAOT publish, known limitations, and platform support.

#### Scenario: Enablement steps are documented
- **WHEN** a developer consults the NativeAOT documentation
- **THEN** they SHALL find steps to enable `PublishAot=true` and any required project configuration

#### Scenario: Limitations and platform matrix are documented
- **WHEN** a developer consults the NativeAOT documentation
- **THEN** they SHALL find known limitations (e.g., platform support, WebView considerations) and a platform support matrix
