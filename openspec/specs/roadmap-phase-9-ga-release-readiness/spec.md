## Purpose

Define Phase 9 (GA Release Readiness) milestones and exit criteria that govern the path from v0.1.x-preview to a 1.0.0 stable release.

## Requirements

### Requirement: Phase 9 defines milestones for 1.0 stable release readiness

The ROADMAP SHALL include a Phase 9 section with at least seven milestones covering API freeze, npm publication, performance re-baseline, changelog, migration guide, and stable release gate. Each milestone SHALL have a defined focus and outcome.

#### Scenario: ROADMAP contains Phase 9 section with milestone table

- **WHEN** the ROADMAP is read
- **THEN** it SHALL contain a `## Phase 9:` header with `(🚧 Active)` status and a milestones table with at least 7 rows

### Requirement: Phase 9 exit criteria are machine-checkable

Phase 9 exit criteria SHALL be expressible as governance assertions that can be validated by the CI pipeline without manual inspection.

#### Scenario: Exit criteria include stable version and evidence requirements

- **WHEN** Phase 9 exit criteria are evaluated
- **THEN** they SHALL require: (1) no preview suffix in package version, (2) npm package published, (3) changelog artifact exists, (4) all governance targets pass

### Requirement: Phase 9 depends on completed Phase 8

Phase 9 SHALL declare Phase 8 as a prerequisite. The dependency graph in ROADMAP SHALL show Phase 8 → Phase 9 connectivity.

#### Scenario: Dependency chain is documented

- **WHEN** the ROADMAP dependency section is read
- **THEN** it SHALL show Phase 8 (✅ Completed) → Phase 9 (🚧 Active) in the dependency graph
