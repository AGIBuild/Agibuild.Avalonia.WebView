## ADDED Requirements

### Requirement: Release orchestration SHALL apply deterministic policy to adoption-readiness findings
Release orchestration gate MUST evaluate adoption-readiness findings using deterministic blocking/advisory policy mapping.

#### Scenario: Blocking adoption finding blocks publication
- **WHEN** adoption readiness includes at least one blocking finding
- **THEN** release decision is `blocked` and publish side effects are prevented

#### Scenario: Advisory-only adoption findings do not force block
- **WHEN** adoption readiness contains advisory findings only and all blocking gates pass
- **THEN** release decision may remain `ready` while advisory diagnostics are preserved in evidence
