# release-orchestration-gate Specification

## Purpose
Define deterministic release-orchestration decision contracts that gate publication side effects on machine-checkable readiness evidence.
## Requirements
### Requirement: Release orchestration SHALL emit deterministic decision state
The release orchestration workflow SHALL emit a machine-checkable decision state with values `ready` or `blocked` based on governed release inputs.

#### Scenario: All governed checks pass
- **WHEN** release orchestration evaluates CI evidence, package validation, strict spec governance, and required quality gates with no blockers
- **THEN** decision state is `ready`

#### Scenario: Any blocking gate fails
- **WHEN** one or more governed release checks fail
- **THEN** decision state is `blocked`

### Requirement: Blocked decisions SHALL provide structured blocking reasons
When decision state is `blocked`, release orchestration MUST produce structured blocking reason entries with stable fields for invariant/category, source artifact, and expected-vs-actual summary.

#### Scenario: Block reason payload is complete
- **WHEN** a release decision is blocked
- **THEN** emitted blocking reasons include deterministic category, source, and expected-vs-actual fields

### Requirement: Publish actions SHALL require ready decision
Release publish actions MUST execute only when release orchestration decision state is `ready`.

#### Scenario: Publish is prevented when blocked
- **WHEN** decision state is `blocked`
- **THEN** publish targets are not executed and pipeline terminates with deterministic diagnostics

### Requirement: Release orchestration SHALL evaluate distribution-readiness input
Release orchestration gate MUST consume distribution-readiness governance output as a first-class decision input for publish readiness.

#### Scenario: Ready decision requires passing distribution input
- **WHEN** release orchestration evaluates publish readiness
- **THEN** decision state can be `ready` only if distribution readiness input is passing

#### Scenario: Distribution failure blocks publication
- **WHEN** distribution readiness input is failing
- **THEN** release orchestration decision is `blocked` with deterministic distribution reason entries

### Requirement: Release orchestration SHALL apply deterministic policy to adoption-readiness findings
Release orchestration gate MUST evaluate adoption-readiness findings using deterministic blocking/advisory policy mapping.

#### Scenario: Blocking adoption finding blocks publication
- **WHEN** adoption readiness includes at least one blocking finding
- **THEN** release decision is `blocked` and publish side effects are prevented

#### Scenario: Advisory-only adoption findings do not force block
- **WHEN** adoption readiness contains advisory findings only and all blocking gates pass
- **THEN** release decision may remain `ready` while advisory diagnostics are preserved in evidence

