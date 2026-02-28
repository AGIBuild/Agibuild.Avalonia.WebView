## ADDED Requirements

### Requirement: Stable publication SHALL require passing distribution readiness
Stable publication workflow MUST require both release orchestration decision `ready` and distribution-readiness status `pass`.

#### Scenario: Stable publication proceeds with passing distribution readiness
- **WHEN** stable tag context is active and distribution-readiness status is pass
- **THEN** stable publication may proceed to push stage

#### Scenario: Stable publication is blocked on distribution failure
- **WHEN** stable tag context is active but distribution-readiness status is fail
- **THEN** workflow fails before push with deterministic distribution diagnostics

### Requirement: Preview publication SHALL record distribution readiness status for auditability
Preview publication flow MUST include distribution-readiness status in release evidence regardless of blocking policy outcome.

#### Scenario: Preview publication records distribution status
- **WHEN** preview publication workflow executes
- **THEN** release evidence contains structured distribution readiness summary and failure/advisory entries when present
