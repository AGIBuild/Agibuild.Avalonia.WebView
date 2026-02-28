# roadmap-phase-transition-management Specification

## Purpose
Define machine-checkable roadmap phase transition and closeout evidence mapping contracts for deterministic governance rollover.
## Requirements
### Requirement: Roadmap phase rollover SHALL remain machine-checkable and deterministic
The roadmap SHALL publish one completed phase id and one active phase id in stable machine-checkable marker format, and phase rollover updates MUST move both markers together as one atomic baseline transition.

#### Scenario: Adjacent phase rollover succeeds
- **WHEN** governance baseline advances from one phase transition pair to the next
- **THEN** roadmap markers expose exactly one updated completed phase id and one updated active phase id in stable marker format

#### Scenario: Partial marker update is rejected
- **WHEN** only completed or active marker is changed during phase rollover
- **THEN** governance assertions fail deterministically before release closeout evidence is accepted

### Requirement: Closeout evidence mapping SHALL align with completed phase artifacts
Roadmap closeout evidence references MUST map to archived changes that belong to the completed phase baseline used by transition governance.

#### Scenario: Completed phase evidence mapping is present
- **WHEN** roadmap phase rollover is declared
- **THEN** roadmap evidence mapping includes closeout archive entries for the completed phase baseline

#### Scenario: Missing closeout mapping fails governance
- **WHEN** completed phase closeout mapping is missing or stale
- **THEN** governance tests fail with deterministic transition consistency diagnostics

