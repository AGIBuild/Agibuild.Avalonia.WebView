# template-shell-presets Specification

## Purpose
TBD - created by archiving change phase4-shell-dx-templates. Update Purpose after archive.
## Requirements
### Requirement: Template exposes explicit shell preset choices
The `agibuild-hybrid` template SHALL expose an explicit shell preset parameter to control generated shell wiring.

#### Scenario: Shell preset choices are discoverable
- **WHEN** template metadata is inspected
- **THEN** shell preset choices include at least `baseline` and `app-shell`, with descriptions for intended usage

### Requirement: App-shell preset scaffolds shell-ready desktop startup
When app-shell preset is selected, the generated desktop host SHALL include shell startup wiring that consumes existing runtime shell contracts.

#### Scenario: App-shell preset emits shell experience bootstrap code
- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop startup source includes shell experience initialization and deterministic disposal hooks

### Requirement: Baseline preset remains minimal
When baseline preset is selected, generated output SHALL omit app-shell bootstrap wiring and remain minimal.

#### Scenario: Baseline preset does not include shell bootstrap
- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `baseline`
- **THEN** generated desktop startup source does not contain app-shell initialization code paths

### Requirement: Preset behavior is governance-testable
Shell preset metadata and wiring markers SHALL be testable in repository governance tests and template E2E flow.

#### Scenario: Governance tests assert shell preset metadata and wiring markers
- **WHEN** repository unit governance tests run
- **THEN** tests validate template metadata contains shell preset symbol and generated source template includes expected preset markers

