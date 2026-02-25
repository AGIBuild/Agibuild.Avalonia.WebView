## ADDED Requirements

### Requirement: Governance tests SHALL enforce phase closeout roadmap status markers
Governance checks SHALL fail when Phase 5 roadmap closeout markers drift from the completed baseline.

#### Scenario: Roadmap closeout marker regression is detected
- **WHEN** roadmap Phase 5 state or evidence mapping markers are removed or changed unexpectedly
- **THEN** governance tests fail with deterministic diagnostics
