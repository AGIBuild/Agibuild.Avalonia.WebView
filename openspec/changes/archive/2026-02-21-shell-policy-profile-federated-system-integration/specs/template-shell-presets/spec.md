## ADDED Requirements

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
