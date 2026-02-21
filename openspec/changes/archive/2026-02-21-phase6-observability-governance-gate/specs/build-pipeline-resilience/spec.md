## ADDED Requirements

### Requirement: CI pipelines enforce repository-wide OpenSpec strict validation
Build governance SHALL provide a dedicated target that executes `openspec validate --all --strict`, and `Ci` plus `CiPublish` MUST depend on this target before completion.

#### Scenario: Strict validation fails in CI target graph
- **WHEN** repository specs violate strict OpenSpec rules
- **THEN** the dedicated strict-validation governance target fails and CI/release targets fail deterministically

#### Scenario: Strict validation passes in CI target graph
- **WHEN** all repository specs satisfy strict OpenSpec rules
- **THEN** the governance target succeeds and CI/release pipelines continue to downstream quality gates
