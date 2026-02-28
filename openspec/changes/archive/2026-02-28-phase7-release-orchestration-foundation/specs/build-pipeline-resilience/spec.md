## ADDED Requirements

### Requirement: CiPublish SHALL evaluate release orchestration gate before publish side effects
`CiPublish` MUST execute release orchestration decision evaluation before any package push or release side-effect target.

#### Scenario: Gate precedes publish targets
- **WHEN** build governance inspects `CiPublish` target graph
- **THEN** release orchestration decision target is in dependency chain before publish side-effect targets

### Requirement: Release blocking failures SHALL be taxonomy-classified
Release orchestration blocking failures MUST be classified into deterministic categories (for example: evidence, package-metadata, governance, quality-threshold) for machine triage.

#### Scenario: Blocking failure includes stable taxonomy
- **WHEN** release orchestration blocks publication
- **THEN** diagnostics include a stable category and actionable source mapping
