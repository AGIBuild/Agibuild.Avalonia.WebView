## ADDED Requirements

### Requirement: App-shell template markers SHALL reflect governance contract v2
The app-shell template SHALL include markerized guidance showing ShowAbout explicit opt-in and revision-aware diagnostics continuity under contract v2 semantics.

#### Scenario: Governance checks verify opt-in marker plus revision-aware diagnostics markers
- **WHEN** governance tests inspect app-shell template source markers
- **THEN** tests assert ShowAbout remains default-deny unless opt-in snippet is enabled and revision-aware diagnostics markers remain present
