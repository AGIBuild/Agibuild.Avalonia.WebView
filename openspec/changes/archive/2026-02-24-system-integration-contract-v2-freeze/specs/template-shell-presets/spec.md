## ADDED Requirements

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
