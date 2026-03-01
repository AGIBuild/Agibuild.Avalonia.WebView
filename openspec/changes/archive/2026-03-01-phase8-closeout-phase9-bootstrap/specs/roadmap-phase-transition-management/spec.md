## MODIFIED Requirements

### Requirement: ROADMAP phase transition markers track completed and active phases

The ROADMAP SHALL contain machine-checkable transition markers identifying exactly one completed phase and one active phase. The completed phase id SHALL be `phase8-bridge-v2-parity` and the active phase id SHALL be `phase9-ga-release-readiness`.

#### Scenario: Transition markers reflect Phase 8 completed and Phase 9 active

- **WHEN** the ROADMAP Phase Transition Status section is read
- **THEN** it SHALL contain `Completed phase id: \`phase8-bridge-v2-parity\``
- **AND** it SHALL contain `Active phase id: \`phase9-ga-release-readiness\``

#### Scenario: Phase 8 header shows completed status

- **WHEN** the ROADMAP Phase 8 section header is read
- **THEN** it SHALL contain `(✅ Completed)` status marker

#### Scenario: Phase 8 closeout evidence lists archived change IDs

- **WHEN** the ROADMAP Phase 8 section is read
- **THEN** it SHALL list OpenSpec archive change IDs covering M8.1–M8.9 milestones
