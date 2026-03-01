## Context

The repository already aligned the host package and project filename (`Agibuild.Fulora.Avalonia`), but the directory remains `src/Agibuild.Fulora`. Path-based identity is still inconsistent, and path assertions in build/test/governance must keep special-case knowledge.

This is a cross-cutting structural refactor affecting solution paths, build scripts, docs metadata sources, and governance assertions. It does not change runtime behavior.

Roadmap/goal alignment:
- Goal IDs: **G4** (testability/governance determinism) and **E1** (template/build consistency).
- Phase context: supports current stabilization/release-governance hardening by removing identity drift in repository-controlled paths.

## Goals / Non-Goals

**Goals:**
- Rename host directory to `src/Agibuild.Fulora.Avalonia`.
- Update all in-repo references to the canonical directory path.
- Keep package/runtime/API behavior unchanged.
- Verify deterministically with targeted tests and strict OpenSpec validation.

**Non-Goals:**
- No compatibility path aliases/symlinks.
- No package ID, namespace, or contract changes.
- No runtime bridge/shell/spa behavior changes.

## Decisions

### Decision 1: Hard directory rename with full reference update
- Chosen: physically rename the directory and update all static path references.
- Alternative rejected: keep old directory with adjusted project filename only; rejected because identity mismatch remains and path governance stays fragile.

### Decision 2: Preserve assembly/runtime contract
- Chosen: only path-level identity alignment; keep assembly output and behavior unchanged.
- Rationale: smallest risk while solving the root problem (repository identity drift).

### Decision 3: Validate via targeted governance and integration tests
- Chosen: run path-sensitive governance tests plus integration project compilation/tests.
- Rationale: directly proves reference consistency after rename.

## Risks / Trade-offs

- [Risk] Hidden hard-coded old directory paths may remain.
  - Mitigation: repository-wide scan for old path token and run affected tests/build.
- [Risk] External scripts outside repo may still use old directory.
  - Mitigation: acceptable hard-cut trade-off; no compatibility layer by explicit policy.

## Migration Plan

1. Rename host directory to `src/Agibuild.Fulora.Avalonia`.
2. Update all repository path references from old to new directory.
3. Run targeted test/governance validations.
4. Verify old path no longer appears in governed repository files.

Rollback: revert the single change set if any blocker is found before release.

## Open Questions

- None; scope and hard-cut policy are explicit.
