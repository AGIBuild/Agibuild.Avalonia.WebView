## ADDED Requirements

### Requirement: App-shell preset SHALL expose v2 tray event payload consumption markers
The app-shell preset SHALL include typed source markers showing tray payload v2 consumption and deterministic field mapping.

#### Scenario: Generated app-shell code includes tray payload v2 mapping markers
- **WHEN** app-shell preset output is generated
- **THEN** generated source includes typed mapping markers for v2 tray core fields and extension fields

### Requirement: App-shell preset SHALL demonstrate `ShowAbout` whitelist behavior
The app-shell preset demo SHALL include explicit outcome rendering for `ShowAbout` action allow/deny under v2 whitelist and policy.

#### Scenario: Template demo renders deterministic deny for blocked `ShowAbout`
- **WHEN** user triggers `ShowAbout` in app-shell demo and policy/whitelist blocks it
- **THEN** web demo renders deterministic deny text using typed outcome fields

### Requirement: Governance tests SHALL enforce no bypass in v2 template flow
Template governance tests SHALL fail if direct platform dispatch is introduced outside typed v2 contracts.

#### Scenario: Governance detects direct platform tray payload passthrough
- **WHEN** repository governance tests detect direct platform payload passthrough markers in template code
- **THEN** CI fails and flags bypass regression
