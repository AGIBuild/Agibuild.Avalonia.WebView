## ADDED Requirements

### Requirement: Stable publication SHALL require release-orchestration ready state
Tag/version semantics alone MUST NOT authorize stable publication; stable package publication SHALL require release-orchestration decision state `ready`.

#### Scenario: Stable tag with ready decision publishes
- **WHEN** nearest tag indicates stable release and release orchestration decision is `ready`
- **THEN** publication workflow proceeds to package push

#### Scenario: Stable tag with blocked decision is rejected
- **WHEN** nearest tag indicates stable release but release orchestration decision is `blocked`
- **THEN** publication workflow fails before push with deterministic blocking diagnostics

### Requirement: Preview publication SHALL surface orchestration state for auditability
Preview publication flows MUST include release-orchestration decision state in generated evidence even when policy permits publishing with non-blocking advisories.

#### Scenario: Preview release records orchestration state
- **WHEN** preview package workflow runs
- **THEN** release evidence includes orchestration decision state and advisory diagnostics mapping
