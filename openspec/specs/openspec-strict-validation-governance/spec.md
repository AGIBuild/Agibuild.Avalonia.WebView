# openspec-strict-validation-governance Specification

## Purpose
Define strict governance rules that keep repository OpenSpec artifacts structurally valid, normatively enforceable, and phase-transition auditable in CI.
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

### Requirement: Spec purpose text SHALL be finalized and non-placeholder
Repository-owned spec files MUST keep `## Purpose` content finalized and descriptive; placeholder tokens such as `TBD`, archive reminder text, or deferred-purpose markers are not allowed in canonical specs.

#### Scenario: Placeholder purpose is detected
- **WHEN** a canonical spec purpose contains `TBD` placeholder text or archive reminder wording
- **THEN** strict governance baseline fails and requires purpose finalization

#### Scenario: Finalized purpose passes strict baseline review
- **WHEN** canonical specs provide explicit purpose statements aligned to capability scope
- **THEN** strict governance baseline remains valid for release and archival workflows

