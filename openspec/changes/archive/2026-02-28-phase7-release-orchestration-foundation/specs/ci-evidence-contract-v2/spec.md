## ADDED Requirements

### Requirement: CI evidence v2 SHALL include release decision summary
Release evidence under contract v2 MUST include a release decision summary section with deterministic `state` and timestamped evaluation context.

#### Scenario: Ready decision summary is present
- **WHEN** release orchestration passes all governed checks
- **THEN** v2 evidence contains decision summary with `state = ready` and non-empty evaluation context

#### Scenario: Blocked decision summary is present
- **WHEN** release orchestration blocks publication
- **THEN** v2 evidence contains decision summary with `state = blocked` and blocking reason references

### Requirement: CI evidence v2 blocked summaries SHALL carry structured reason entries
When release decision summary state is `blocked`, the evidence payload MUST include structured blocking reason entries with category, invariant/source reference, and expected-vs-actual fields.

#### Scenario: Blocking reasons are machine-auditable
- **WHEN** consumers parse blocked release evidence
- **THEN** they can deterministically identify blocking categories and sources without free-text parsing
