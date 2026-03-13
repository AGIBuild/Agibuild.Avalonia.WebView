## 1. Version authority cutover (Deliverables D1/D2)

- [x] 1.1 [D1] Update shared repo version source to baseline `1.5` in central build properties and remove conflicting tag-first version authority inputs (AC: all packable projects resolve baseline major/minor from one source).
- [x] 1.2 [D1] Remove MinVer package references/properties from active projects and central build props (AC: active build graph has no MinVer dependency and no MinVer-only fallback branch).
- [x] 1.3 [D2] Implement CI version computation utility producing `X.Y.Z.<run_number>` and wire it into pack/publish version inputs (AC: computed version is numeric four-part and contains no `ci` text suffix).

## 2. Build orchestration and provenance manifest (Deliverables D3/D4)

- [x] 2.1 [D3] Refactor build targets so CI and release depend on one readiness contract graph with no release-only quality checks (AC: dependency graph parity is machine-checkable).
- [x] 2.2 [D3] Remove MinVer-specific build parameters/messages from orchestration code and validation checks (AC: build logic no longer references MinVer semantics).
- [x] 2.3 [D4] Add CI artifact provenance manifest generation containing version, commit SHA, run id, and artifact hashes (AC: manifest is uploaded with CI artifact bundle and consumed by release lane).

## 3. Workflow one-step cutover (Deliverables D5/D6)

- [x] 3.1 [D5] Merge CI and release into one workflow with staged jobs (`ci` -> `release`) and artifact handoff in same run (AC: release job depends on successful CI job).
- [x] 3.2 [D5] Configure release job protected environment approval gate (required reviewers) so publish steps run only after manual approval (AC: release job enters waiting-for-approval state after CI success).
- [x] 3.3 [D5] Ensure release stage performs download + verify + publish only, with explicit no-rebuild policy (AC: unified workflow release stage contains no package build/pack step).
- [x] 3.4 [D6] Remove `.github/workflows/release.yml` and remove `create-tag.yml` from release version authority path (AC: release version is sourced exclusively from CI manifest path in unified workflow).

## 4. Governance assertions and test coverage (Deliverables D7)

- [x] 4.1 [D7] Extend governance invariants for version provenance parity and no-rebuild promotion policy (AC: deterministic invariant IDs emitted on mismatch).
- [x] 4.2 [D7] Add governance assertion for "no MinVer authority in active build graph" (AC: deterministic failure if MinVer config/reference appears in active source/workflow paths).
- [x] 4.3 [D7] Update unit/automation governance tests for shared readiness dependencies, baseline `1.5`, and `X.Y.Z.<run_number>` format checks (AC: tests fail on drift and pass on compliant graph).
- [x] 4.4 [D7] Add integration verification for CI artifact -> release promotion path reuse (AC: promotion flow proves version equality and artifact hash continuity).
