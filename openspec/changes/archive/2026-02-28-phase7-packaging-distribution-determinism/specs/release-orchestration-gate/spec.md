## ADDED Requirements

### Requirement: Release orchestration SHALL evaluate distribution-readiness input
Release orchestration gate MUST consume distribution-readiness governance output as a first-class decision input for publish readiness.

#### Scenario: Ready decision requires passing distribution input
- **WHEN** release orchestration evaluates publish readiness
- **THEN** decision state can be `ready` only if distribution readiness input is passing

#### Scenario: Distribution failure blocks publication
- **WHEN** distribution readiness input is failing
- **THEN** release orchestration decision is `blocked` with deterministic distribution reason entries
