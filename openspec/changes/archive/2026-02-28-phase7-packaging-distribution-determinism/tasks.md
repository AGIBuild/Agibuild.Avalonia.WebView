## 1. Spec and governance baseline alignment (Deliverable M7.2)

- [x] 1.1 Finalize capability/spec mapping for `release-distribution-determinism` and modified capabilities; acceptance: delta spec set passes `openspec validate --all --strict`.
- [x] 1.2 Resolve archived-spec Purpose placeholders introduced in prior archive sync for Phase 7 governance continuity; acceptance: no `TBD - created by archiving change` remains in touched main specs.

## 2. Distribution-readiness artifact implementation (Deliverable M7.2)

- [x] 2.1 Implement deterministic distribution-readiness report emission in build governance pipeline; acceptance: report artifact includes schema version, lane context, and structured failures.
- [x] 2.2 Enforce canonical package set completeness checks for publish lane; acceptance: missing canonical package yields deterministic blocking diagnostics.
- [x] 2.3 Enforce stable metadata policy assertions in distribution-readiness flow; acceptance: stable metadata policy violation is captured as expected-vs-actual failure entry.

## 3. Release evidence and gate integration (Deliverable M7.2)

- [x] 3.1 Extend CI evidence v2 payload with distribution-readiness summary and failure entries; acceptance: closeout snapshot includes machine-readable distribution section.
- [x] 3.2 Integrate distribution-readiness input into `ReleaseOrchestrationGovernance`; acceptance: failed distribution readiness forces `blocked` decision with stable taxonomy.

## 4. Regression tests and verification (Deliverable M7.2)

- [x] 4.1 Add/extend governance tests for distribution report schema, gate sequencing, and stable/preview policy behavior; acceptance: targeted and full test suites pass.
- [x] 4.2 Run verification baseline (`nuke Test`, `nuke Coverage`, `nuke ReleaseOrchestrationGovernance`, `openspec validate --all --strict`); acceptance: all commands pass and evidence artifacts are generated.
