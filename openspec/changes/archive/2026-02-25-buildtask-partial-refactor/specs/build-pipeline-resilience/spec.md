## ADDED Requirements

### Requirement: Build entry bootstrap SHALL remain deterministic after script modularization
Build script modularization SHALL preserve deterministic Nuke bootstrap behavior and default target dispatch.

#### Scenario: Entry class rename keeps default target execution stable
- **WHEN** build entry class is renamed for clarity
- **THEN** Nuke bootstrap still executes the default build target deterministically
- **AND** target invocation contracts remain unchanged for existing scripts/CI

### Requirement: Build orchestration source SHALL be responsibility-partitioned
Build orchestration implementation SHALL be partitioned into cohesive partial files to improve maintainability without changing target semantics.

#### Scenario: Partial split preserves CI target graph behavior
- **WHEN** build orchestration source is split by responsibilities
- **THEN** `Test`, `Coverage`, `Ci`, and `CiPublish` target behavior and dependencies remain compatible
- **AND** governance checks can still verify critical target and artifact contracts
