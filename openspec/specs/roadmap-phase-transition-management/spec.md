# roadmap-phase-transition-management Specification

## Purpose
Define machine-checkable roadmap phase transition and closeout evidence mapping contracts for deterministic governance rollover.
## Requirements
### Requirement: ROADMAP phase transition markers track completed and active phases

The ROADMAP SHALL contain machine-checkable transition markers identifying exactly one completed phase and one active phase. The completed phase id SHALL be `phase12-enterprise-advanced-scenarios` and the active phase id SHALL be `post-roadmap-maintenance`.

#### Scenario: Transition markers reflect Phase 12 completed and post-roadmap active

- **WHEN** the ROADMAP Phase Transition Status section is read
- **THEN** it SHALL contain `Completed phase id: \`phase12-enterprise-advanced-scenarios\``
- **AND** it SHALL contain `Active phase id: \`post-roadmap-maintenance\``

#### Scenario: Phase 12 header shows completed status

- **WHEN** the ROADMAP Phase 12 section header is read
- **THEN** it SHALL contain `(✅ Completed)` status marker

#### Scenario: Phase 12 closeout evidence lists archived change IDs

- **WHEN** the ROADMAP Phase 12 section is read
- **THEN** it SHALL list OpenSpec archive change IDs covering M12.1–M12.4 milestones

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

