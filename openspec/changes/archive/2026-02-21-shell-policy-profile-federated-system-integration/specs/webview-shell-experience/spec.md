## ADDED Requirements

### Requirement: Shell pruning pipeline SHALL apply profile-policy federation order deterministically
The shell experience component SHALL evaluate menu pruning in deterministic order: permission profile decision, shell policy decision, then effective menu state mutation.

#### Scenario: Profile stage deny short-circuits policy stage
- **WHEN** profile federation stage returns deny for pruning request
- **THEN** shell policy stage is skipped and effective menu state remains unchanged

#### Scenario: Policy stage deny after profile allow prevents mutation
- **WHEN** profile stage allows and shell policy stage denies pruning request
- **THEN** runtime does not mutate effective menu state and emits explicit stage-specific diagnostics

### Requirement: Federated pruning diagnostics SHALL be stage-attributable
Shell experience diagnostics SHALL identify federation stage, decision source, and final deterministic outcome for each pruning evaluation.

#### Scenario: Equivalent stage decisions emit equivalent diagnostics
- **WHEN** equivalent profile and policy decisions are produced for repeated pruning evaluations
- **THEN** diagnostics fields for stage attribution and final outcome remain stable across runs
