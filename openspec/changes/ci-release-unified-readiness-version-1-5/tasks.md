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

## 5. Version management command (Deliverables D8)

- [ ] 5.1 [D8] Implement Nuke `UpdateVersion` target in `build/Build.Versioning.cs` (AC: reads current `VersionPrefix` from `Directory.Build.props`, auto-increments patch when no parameter, writes back updated version).
- [ ] 5.2 [D8] Add `--update-version-to` parameter accepting `X.Y.Z` format with strict greater-than validation (AC: rejects versions not strictly greater than current, returns non-zero exit code on failure).
- [ ] 5.3 [D8] Add unit tests for UpdateVersion logic covering auto-increment and comparison scenarios.

## 6. Test-before-pack ordering (Deliverables D9)

- [ ] 6.1 [D9] Update `Pack` target dependency chain to include `Coverage` and `AutomationLaneReport` as prerequisites (AC: `Pack` cannot execute before tests complete).
- [ ] 6.2 [D9] Update governance tests to assert Pack depends on test targets.

## 7. Release stage consolidation (Deliverables D10)

- [ ] 7.1 [D10] Convert `docs-deploy.yml` to `workflow_call` entry point, remove independent push/dispatch triggers (AC: docs deploy only runs when called from unified workflow).
- [ ] 7.2 [D10] Add documentation deployment step to release job in `ci.yml` via `workflow_call` or inline (AC: docs deploy after approval in same run).
- [ ] 7.3 [D10] Add Git tag creation step (`vX.Y.Z.<run_number>`) in release job (AC: tag created after successful package publish).
- [ ] 7.4 [D10] Add GitHub Release creation step in release job (AC: release created with tag, auto-generated notes).
- [ ] 7.5 [D10] Delete `create-tag.yml` workflow file (AC: file removed, no dangling references).

## 8. Release environment protection (Deliverables D11)

- [ ] 8.1 [D11] Configure `release` GitHub environment with required reviewers (AC: `gh api` shows non-empty `protection_rules` with `required_reviewers`).
- [ ] 8.2 [D11] Verify release job enters `Waiting for approval` state after CI success (AC: manual approval required before publish steps execute).

## 9. Updated governance assertions (Deliverables D12)

- [ ] 9.1 [D12] Update governance tests to remove assertion for `create-tag.yml` existence.
- [ ] 9.2 [D12] Add governance assertion for test-before-pack ordering in build target graph.
- [ ] 9.3 [D12] Add governance assertion for release job containing docs/tag/release steps.
- [ ] 9.4 [D12] Add governance assertion for release environment protection rules.
