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

### Requirement: App-shell preset SHALL demonstrate explicit ShowAbout allowlist wiring
The app-shell preset SHALL include deterministic markers showing how `ShowAbout` is enabled only through explicit allowlist configuration.

#### Scenario: App-shell marker distinguishes allowlisted vs default deny path
- **WHEN** template governance checks inspect app-shell preset source markers
- **THEN** markers show explicit `ShowAbout` allowlist registration path and default deny semantics

### Requirement: App-shell preset SHALL demonstrate bounded tray metadata consumption
The app-shell preset SHALL demonstrate web-side consumption of canonical tray semantic fields and bounded metadata envelope without raw platform payload bypass.

#### Scenario: Governance checks reject raw payload passthrough marker
- **WHEN** repository governance tests scan app-shell template markers
- **THEN** tests fail if markers indicate direct raw platform payload passthrough to web

### Requirement: App-shell preset SHALL demonstrate federated menu pruning behavior
The app-shell preset SHALL include markerized flow demonstrating profile + policy federated pruning decision before effective menu state update.

#### Scenario: Template demo exposes federated pruning result path
- **WHEN** app-shell template demo runs menu pruning workflow
- **THEN** web demo receives typed pruning result containing stage-attributable federated decision metadata

### Requirement: App-shell template SHALL expose explicit ShowAbout opt-in snippet marker
The app-shell template SHALL include deterministic markerized snippet showing how to opt in `ShowAbout` via explicit system action allowlist while default configuration remains deny-by-default.

#### Scenario: Governance checks verify opt-in marker and default deny path
- **WHEN** governance tests inspect app-shell preset source markers
- **THEN** tests assert both presence of ShowAbout opt-in snippet marker and absence of default ShowAbout allowlisting

### Requirement: Template flow SHALL keep bounded metadata handling markerized
The app-shell template SHALL preserve bounded metadata envelope consumption markers for host-originated events when ShowAbout opt-in guidance is added.

#### Scenario: ShowAbout guidance does not introduce raw metadata bypass
- **WHEN** template source includes ShowAbout allowlist guidance updates
- **THEN** governance tests continue to pass bounded metadata consumption markers and reject raw payload passthrough markers

### Requirement: App-shell template markers SHALL reflect governance contract v2
The app-shell template SHALL include markerized guidance showing ShowAbout explicit opt-in and revision-aware diagnostics continuity under contract v2 semantics.

#### Scenario: Governance checks verify opt-in marker plus revision-aware diagnostics markers
- **WHEN** governance tests inspect app-shell template source markers
- **THEN** tests assert ShowAbout remains default-deny unless opt-in snippet is enabled and revision-aware diagnostics markers remain present

### Requirement: App-shell preset SHALL expose v2 tray event payload consumption markers
The app-shell preset SHALL include typed source markers showing tray payload v2 consumption and deterministic field mapping.

#### Scenario: Generated app-shell code includes tray payload v2 mapping markers
- **WHEN** app-shell preset output is generated
- **THEN** generated source includes typed mapping markers for v2 tray core fields and extension fields

### Requirement: App-shell preset SHALL demonstrate ShowAbout whitelist behavior
The app-shell preset demo SHALL include explicit outcome rendering for ShowAbout action allow/deny under v2 whitelist and policy.

#### Scenario: Template demo renders deterministic deny for blocked ShowAbout
- **WHEN** user triggers ShowAbout in app-shell demo and policy/whitelist blocks it
- **THEN** web demo renders deterministic deny text using typed outcome fields

### Requirement: Governance tests SHALL enforce no bypass in v2 template flow
Template governance tests SHALL fail if direct platform dispatch is introduced outside typed v2 contracts.

#### Scenario: Governance detects direct platform tray payload passthrough
- **WHEN** repository governance tests detect direct platform payload passthrough markers in template code
- **THEN** CI fails and flags bypass regression

### Requirement: App-shell preset SHALL expose runtime ShowAbout opt-in strategy
The app-shell preset SHALL keep ShowAbout default-deny and expose a deterministic runtime opt-in toggle marker for host configuration.

#### Scenario: Default template keeps ShowAbout disabled
- **WHEN** app-shell preset runs with no ShowAbout toggle configured
- **THEN** template whitelist does not include ShowAbout and demo returns deterministic deny text

#### Scenario: Runtime toggle marker enables ShowAbout path
- **WHEN** host enables the documented runtime toggle marker
- **THEN** template whitelist includes ShowAbout and bridge path executes policy-governed action flow

### Requirement: Template v2 event markers SHALL include canonical timestamp consumption
The app-shell template SHALL consume and display canonical `OccurredAtUtc` payload from typed inbound events.

#### Scenario: Demo output includes canonical timestamp marker
- **WHEN** user drains inbound system-integration events in app-shell demo
- **THEN** output includes canonical timestamp field and bounded metadata markers without direct platform payload bypass

### Requirement: App-shell demo SHALL provide strategy visualization for system integration
The template app-shell demo SHALL render deterministic strategy output that makes policy and whitelist outcomes machine-checkable.

#### Scenario: Strategy panel shows ShowAbout outcome state
- **WHEN** user triggers ShowAbout action from app-shell demo
- **THEN** panel output includes mode, action, outcome, and deny/failure reason fields

### Requirement: App-shell demo SHALL provide one-click ShowAbout scenario switching
The template SHALL expose deterministic controls for switching between deny-default and explicit allow scenarios without changing source code.

#### Scenario: Scenario switch updates ShowAbout execution branch
- **WHEN** developer toggles the scenario control and re-runs ShowAbout action
- **THEN** result deterministically reflects the selected scenario branch

### Requirement: Template SHALL expose reusable regression check script
The template web bundle SHALL expose a reusable regression check function that runs canonical system-integration demo checks and returns structured results.

#### Scenario: Regression script returns structured checks
- **WHEN** automation invokes the template regression function
- **THEN** function returns machine-readable result entries with stable keys and pass/fail state

