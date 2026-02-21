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
When app-shell preset is selected, the generated desktop host SHALL include shell startup wiring that consumes existing runtime shell contracts and includes typed system integration wiring for menu/tray baseline flows.

#### Scenario: App-shell preset emits shell experience bootstrap code
- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop startup source includes shell experience initialization and deterministic disposal hooks

#### Scenario: App-shell preset emits system integration bootstrap markers
- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated source contains typed system integration registration markers for menu/tray capability flow

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

### Requirement: App-shell preset wires shortcut routing with deterministic lifecycle cleanup
When app-shell preset is selected, generated desktop host SHALL wire a reusable shortcut router to window key input and detach it during preset disposal.

#### Scenario: App-shell preset enables default shell shortcuts
- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop shell preset code initializes shortcut routing and handles mapped shortcuts through router execution

#### Scenario: App-shell preset detaches shortcut handler on disposal
- **WHEN** generated desktop host unloads and shell preset is disposed
- **THEN** key input handler for shortcut routing is detached deterministically

### Requirement: App-shell preset SHALL demonstrate Web-first system integration flow
The app-shell preset SHALL demonstrate a canonical flow from web call to typed bridge, capability gateway, policy evaluation, and typed system integration result.

#### Scenario: Template includes menu or tray typed service exposure
- **WHEN** app-shell preset output is generated
- **THEN** desktop host exposes a typed bridge service that routes system integration operations through shell capability governance

#### Scenario: Governance tests validate system integration preset markers
- **WHEN** repository governance tests run
- **THEN** tests deterministically assert presence of system integration preset markers and absence of direct platform API bypass markers

### Requirement: App-shell preset SHALL demonstrate system integration bidirectional flow
The app-shell preset SHALL demonstrate a canonical bidirectional flow where web issues typed system integration commands and receives typed host-originated system integration events.

#### Scenario: App-shell template includes inbound event wiring markers
- **WHEN** app-shell preset output is generated
- **THEN** generated source contains typed inbound system integration event wiring markers in addition to outbound command markers

### Requirement: App-shell preset SHALL demonstrate policy-driven menu pruning result path
The app-shell preset SHALL include a minimal demo path showing menu state result produced by policy-governed pruning.

#### Scenario: Template web demo consumes pruned menu state
- **WHEN** app-shell template demo executes menu update flow
- **THEN** web-side demo consumes typed pruned menu state result rather than direct platform menu state

### Requirement: Governance tests SHALL enforce no bypass for bidirectional template flow
Template governance tests SHALL assert that both outbound command and inbound event paths use typed shell capability contracts without direct platform bypass markers.

#### Scenario: Governance test detects bypass marker regression
- **WHEN** repository governance tests scan template source markers
- **THEN** tests fail if direct platform dispatch markers appear outside typed shell capability path

