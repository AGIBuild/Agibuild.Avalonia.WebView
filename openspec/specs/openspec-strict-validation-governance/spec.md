# openspec-strict-validation-governance Specification

## Purpose
TBD - created by archiving change openspec-strict-validation-debt-cleanup. Update Purpose after archive.
## Requirements
### Requirement: Repository specs comply with strict OpenSpec structure
All repository-owned spec files SHALL include a `## Purpose` section and a `## Requirements` section in strict-compatible format.

#### Scenario: Missing purpose or requirements is rejected
- **WHEN** a spec file omits `## Purpose` or `## Requirements`
- **THEN** strict validation fails with a structural error

### Requirement: Normative requirements use enforceable wording
Each requirement statement SHALL include normative wording (`SHALL` or `MUST`) to ensure deterministic policy and contract interpretation.

#### Scenario: Non-normative requirement text is rejected
- **WHEN** a requirement lacks `SHALL` or `MUST`
- **THEN** strict validation fails with a normative-language error

### Requirement: Every requirement is scenario-backed
Each requirement SHALL include at least one scenario block using `#### Scenario:` with WHEN/THEN assertions.

#### Scenario: Requirement without scenario is rejected
- **WHEN** a requirement block has no `#### Scenario:` entries
- **THEN** strict validation fails with a missing-scenario error

### Requirement: Full-repository strict validation is release-governed
Repository governance MUST require `openspec validate --all --strict` to pass before a strict-governance cleanup change is considered complete.

#### Scenario: Strict baseline is green
- **WHEN** the validation command runs on repository specs
- **THEN** all items pass with zero failures

### Requirement: Governance tests SHALL enforce phase closeout roadmap status markers
Governance checks SHALL fail when Phase 5 roadmap closeout markers drift from the completed baseline.

#### Scenario: Roadmap closeout marker regression is detected
- **WHEN** roadmap Phase 5 state or evidence mapping markers are removed or changed unexpectedly
- **THEN** governance tests fail with deterministic diagnostics

