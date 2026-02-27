## 1. Build Closeout Contract Cutover

- [x] 1.1 Replace phase-specific closeout snapshot target/output naming in `build/Build.Governance.cs` with phase-neutral contract terms (Deliverable: `build-pipeline-resilience`; AC: no `phase5-closeout-snapshot` contract token remains in governed closeout output path/payload).
- [x] 1.2 Add transition scope metadata (`completedPhase`, `activePhase`) to closeout snapshot payload and enforce deterministic producer metadata (Deliverable: `ci-evidence-contract-v2`; AC: snapshot JSON includes normalized transition fields and schema v2 remains valid).
- [x] 1.3 Update build entry constants/target dependencies in `build/Build.cs` to reference the new closeout contract target name consistently (Deliverable: `build-pipeline-resilience`; AC: `Ci`/`CiPublish` dependency graph resolves without old target identifiers).

## 2. Governance Semantic Assertion Upgrade

- [x] 2.1 Refactor `AutomationLaneGovernanceTests` to replace `Phase5` literal-coupled assertions with invariant-driven semantic checks (Deliverable: `governance-semantic-assertions`; AC: tests assert transition invariants via stable IDs, not phase heading literals).
- [x] 2.2 Add deterministic failure diagnostics for transition invariant violations including invariant id and expected-vs-actual fields (Deliverable: `governance-semantic-assertions`; AC: failing assertions surface machine-actionable diagnostics).
- [x] 2.3 Update evidence contract assertions to require transition metadata fields in closeout snapshot validation (Deliverable: `ci-evidence-contract-v2`; AC: missing `completedPhase` or `activePhase` fails deterministically).

## 3. Roadmap Transition Governance Alignment

- [x] 3.1 Update `openspec/ROADMAP.md` to declare the active post-Phase-5 stage and align closeout references with phase-neutral snapshot naming (Deliverable: `electron-replacement-foundation`; AC: roadmap contains completed previous phase + explicit active next phase with traceable evidence links).
- [x] 3.2 Align roadmap-facing governance references in tests and build evidence mapping to the updated transition model (Deliverable: `electron-replacement-foundation` + `build-pipeline-resilience`; AC: roadmap consistency tests pass without phase-number hardcoding).

## 4. Verification and Readiness

- [x] 4.1 Run targeted governance test suite for closeout and transition invariants (Deliverable: all; AC: updated governance tests pass).
- [x] 4.2 Run `openspec validate --all --strict` and resolve all validation errors (Deliverable: all; AC: strict validation returns zero failures).
- [x] 4.3 Run release-path governance validation (`nuke CiPublish` or equivalent governed subset) to verify closeout contract continuity (Deliverable: all; AC: release governance gates pass with new phase-neutral snapshot contract).
