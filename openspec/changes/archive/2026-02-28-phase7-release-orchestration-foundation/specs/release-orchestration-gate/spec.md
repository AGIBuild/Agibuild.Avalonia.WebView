## ADDED Requirements

### Requirement: Release orchestration SHALL emit deterministic decision state
The release orchestration workflow SHALL emit a machine-checkable decision state with values `ready` or `blocked` based on governed release inputs.

#### Scenario: All governed checks pass
- **WHEN** release orchestration evaluates CI evidence, package validation, strict spec governance, and required quality gates with no blockers
- **THEN** decision state is `ready`

#### Scenario: Any blocking gate fails
- **WHEN** one or more governed release checks fail
- **THEN** decision state is `blocked`

### Requirement: Blocked decisions SHALL provide structured blocking reasons
When decision state is `blocked`, release orchestration MUST produce structured blocking reason entries with stable fields for invariant/category, source artifact, and expected-vs-actual summary.

#### Scenario: Block reason payload is complete
- **WHEN** a release decision is blocked
- **THEN** emitted blocking reasons include deterministic category, source, and expected-vs-actual fields

### Requirement: Publish actions SHALL require ready decision
Release publish actions MUST execute only when release orchestration decision state is `ready`.

#### Scenario: Publish is prevented when blocked
- **WHEN** decision state is `blocked`
- **THEN** publish targets are not executed and pipeline terminates with deterministic diagnostics
