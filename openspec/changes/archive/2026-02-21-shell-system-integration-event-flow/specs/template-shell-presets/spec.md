## ADDED Requirements

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
