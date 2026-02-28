# phase-baseline-reconciliation Specification

## Purpose
TBD - created by archiving change phase7-closeout-phase8-reconciliation. Update Purpose after archive.
## Requirements
### Requirement: Roadmap and governance transition baselines SHALL be reconciled atomically
Repository transition baseline updates MUST change roadmap machine-checkable markers and governance closeout constants as one atomic operation.

#### Scenario: Atomic baseline reconciliation succeeds
- **WHEN** completed and active phase identifiers are advanced to the next adjacent transition pair
- **THEN** roadmap markers and governance closeout constants expose the same phase pair without intermediate drift

#### Scenario: Partial reconciliation is rejected
- **WHEN** roadmap markers are updated but governance closeout constants are not (or vice versa)
- **THEN** transition governance fails deterministically before release-readiness evidence is accepted

### Requirement: Completed-phase closeout archive mapping SHALL match the reconciled baseline
The set of completed-phase closeout archive identifiers included in closeout evidence MUST correspond to the completed phase selected by the reconciled transition baseline.

#### Scenario: Completed-phase archive mapping is synchronized
- **WHEN** transition baseline reconciliation sets a completed phase
- **THEN** closeout evidence archive mappings reference archived changes belonging to that completed phase baseline

#### Scenario: Stale archive mapping fails reconciliation checks
- **WHEN** closeout archive mappings still reference an older completed phase after baseline reconciliation
- **THEN** governance reports deterministic expected-vs-actual diagnostics and blocks release orchestration readiness

