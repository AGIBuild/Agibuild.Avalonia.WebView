## ADDED Requirements

### Requirement: Phase closeout governance SHALL detect diagnostic and template regression entry points
Shell production closeout governance SHALL assert that diagnostic export and template regression entry points remain detectable in repository sources.

#### Scenario: Entry-point marker removal fails governance
- **WHEN** diagnostic export marker or template regression entry function marker is removed
- **THEN** governance tests fail with deterministic signal
