## 1. Capability and policy framing (Deliverable M7.3)

- [x] 1.1 Finalize `adoption-readiness-signals` capability contract and modified capability deltas; acceptance: spec set is internally consistent and passes strict OpenSpec validation.
- [x] 1.2 Define deterministic blocking vs advisory policy mapping for adoption findings; acceptance: policy tiers are explicitly documented in specs with scenario coverage.

## 2. Adoption-readiness evidence implementation (Deliverable M7.3)

- [x] 2.1 Implement adoption-readiness governance report generation in CI flow; acceptance: report contains deterministic KPI dimensions (docs/templates/runtime) and provenance fields.
- [x] 2.2 Implement structured adoption finding entries with source mapping and expected-vs-actual diagnostics; acceptance: non-passing signals produce machine-readable entries.

## 3. Evidence and orchestration integration (Deliverable M7.3)

- [x] 3.1 Extend CI evidence v2 payload with adoption readiness summary and finding entries; acceptance: closeout snapshot and related evidence include adoption section.
- [x] 3.2 Integrate adoption findings into release-orchestration decision policy; acceptance: blocking findings force `blocked`, advisory-only findings are preserved without forced block.

## 4. Governance tests and verification (Deliverable M7.3)

- [x] 4.1 Add/extend governance tests for adoption evidence schema and policy behavior; acceptance: tests assert deterministic parsing and policy outcomes.
- [x] 4.2 Run verification baseline (`nuke Test`, `nuke Coverage`, `nuke ReleaseOrchestrationGovernance`, `openspec validate --all --strict`); acceptance: all checks pass with adoption evidence present.
