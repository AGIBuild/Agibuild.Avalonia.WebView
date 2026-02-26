## 1. Phase 6 Foundation Alignment

- [x] 1.1 Confirm and document Phase 6 transitional scope against `PROJECT.md` goals (G3/G4) and current `ROADMAP.md` gap (Deliverable: governance-semantic-assertions, AC: scope note recorded in change artifacts).
- [x] 1.2 Define invariant catalog IDs for governance checks (Deliverable: governance-semantic-assertions, AC: each invariant has stable id + owner + target artifact mapping).

## 2. Semantic Governance Assertion Framework

- [x] 2.1 Add shared semantic assertion helpers for JSON/graph invariants in testing infrastructure (Deliverable: governance-semantic-assertions, AC: helper APIs cover pass/fail/assert-diagnostic flows).
- [x] 2.2 Refactor `AutomationLaneGovernanceTests` to consume shared semantic helpers for governed invariants (Deliverable: governance-semantic-assertions, AC: brittle textual checks replaced for targeted invariants).
- [x] 2.3 Add deterministic diagnostic formatting for invariant failures (Deliverable: governance-semantic-assertions, AC: failure output includes invariant id, artifact path, expected-vs-actual).

## 3. CI Evidence Contract v2

- [x] 3.1 Define v2 evidence schema model and required provenance fields (Deliverable: ci-evidence-contract-v2, AC: schemaVersion/provenance requirements codified and testable).
- [x] 3.2 Update release evidence producers in build orchestration to emit contract v2 payloads (Deliverable: ci-evidence-contract-v2 + build-pipeline-resilience, AC: `CiPublish` outputs validate as v2).
- [x] 3.3 Update runtime critical-path governance to require v2 provenance continuity (Deliverable: runtime-automation-validation, AC: missing provenance fails deterministically).
- [x] 3.4 Update shell matrix/runtime consistency governance to use semantic invariant source and v2-compatible evidence links (Deliverable: shell-production-validation, AC: bidirectional mismatch fails with invariant id).

## 4. Bridge Distribution Governance Hardening

- [x] 4.1 Add package-manager parity smoke checks for npm/pnpm/yarn in `CiPublish` governance lane (Deliverable: bridge-npm-distribution, AC: each manager executes install/build/consume smoke path).
- [x] 4.2 Add Node LTS compatibility matrix smoke checks for bridge package consumption in `CiPublish` (Deliverable: bridge-npm-distribution, AC: declared LTS set passes typed import/invocation smoke).
- [x] 4.3 Emit machine-readable bridge distribution governance report for CI consumption (Deliverable: bridge-npm-distribution + ci-evidence-contract-v2, AC: report captured and linked in v2 evidence provenance).

## 5. Verification and Release Rollout

- [x] 5.1 Run targeted unit/integration/governance test suites for changed invariants and schema checks (Deliverable: all, AC: targeted suites pass with deterministic diagnostics).
- [x] 5.2 Run `openspec validate --all --strict` and resolve all validation issues (Deliverable: all, AC: zero strict validation failures).
- [x] 5.3 Execute `nuke CiPublish` dry-run validation for new governance paths (Deliverable: build-pipeline-resilience + ci-evidence-contract-v2, AC: release gates pass and v2 evidence artifacts are generated).
