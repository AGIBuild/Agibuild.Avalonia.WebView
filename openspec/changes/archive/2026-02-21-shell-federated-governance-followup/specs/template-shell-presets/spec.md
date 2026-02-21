## ADDED Requirements

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
