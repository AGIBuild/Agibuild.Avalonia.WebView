## MODIFIED Requirements

### Requirement: Closeout evidence mapping SHALL align with completed phase artifacts
Roadmap closeout evidence references MUST map to archived changes that belong to the completed phase baseline used by transition governance, and MUST remain synchronized with the release closeout snapshot transition baseline constants.

#### Scenario: Completed phase evidence mapping is present
- **WHEN** roadmap phase rollover is declared
- **THEN** roadmap evidence mapping includes closeout archive entries for the completed phase baseline

#### Scenario: Missing closeout mapping fails governance
- **WHEN** completed phase closeout mapping is missing or stale
- **THEN** governance tests fail with deterministic transition consistency diagnostics

#### Scenario: Closeout snapshot baseline drift is rejected
- **WHEN** roadmap completed-phase evidence mapping is updated but closeout snapshot transition constants still reference the previous baseline
- **THEN** governance fails deterministically before release closeout evidence is accepted
