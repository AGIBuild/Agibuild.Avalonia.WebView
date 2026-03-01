## Context

The repository completed a hard cut where the Avalonia host package identity is `Agibuild.Fulora.Avalonia`, but the host project file remains `Agibuild.Fulora.csproj`. Several build/governance/test paths can infer identity from project file names, so this mismatch increases maintenance cost and path-fragility.

This change is a cross-cutting packaging/governance alignment effort touching build references, solution entries, and path-based assertions. It supports deterministic automation and aligns with project goals around governed reliability and testability.

Roadmap alignment:
- Goal ID: **G4 (Contract-Driven Testability)** and **E1 (Project Template)**.
- Phase alignment: supports current stabilization/governance hardening work by keeping release and template surfaces deterministic.

## Goals / Non-Goals

**Goals:**
- Align Avalonia host project filename with package identity (`Agibuild.Fulora.Avalonia`).
- Eliminate old project-file path usage from governed surfaces.
- Preserve runtime and package behavior while reducing identity ambiguity in CI/build/test automation.
- Keep validation deterministic with explicit tests/governance checks.

**Non-Goals:**
- No compatibility layers for old project file names.
- No package ID or API contract changes.
- No bridge/runtime/shell feature changes.

## Decisions

### Decision 1: Hard rename project file and update all in-repo references
- Chosen: rename `Agibuild.Fulora.csproj` to `Agibuild.Fulora.Avalonia.csproj` and update all static references.
- Alternatives considered:
  - Keep file name as-is and rely on `PackageId` only: rejected due to persistent naming drift and path ambiguity.
  - Add alias/symlink/duplicate project file: rejected due to complexity and non-deterministic tooling behavior across environments.

### Decision 2: Treat this as identity alignment, not behavioral change
- Chosen: keep runtime logic unchanged and only update file-level identity and references.
- Rationale: reduces risk and keeps scope focused while still addressing root cause (identity inconsistency).

### Decision 3: Validate with targeted tests plus repository-wide path scan
- Chosen: run impacted unit/governance tests and ensure no stale references to the old `.csproj` path.
- Rationale: direct verification of the changed concern (path identity), aligned with G4.

## Risks / Trade-offs

- [Risk] Hidden hard-coded paths in less common automation scripts may be missed.
  - Mitigation: repository-wide search for old filename and run governance/packaging-related tests.
- [Risk] External user scripts outside repository may still reference old file name.
  - Mitigation: keep change internal-only and document in release notes when this change is released.
- [Trade-off] Hard cut improves determinism but can break consumers relying on old path.
  - Mitigation: explicitly accepted by request; no compatibility layer added.

## Migration Plan

1. Rename the host project file.
2. Update all repository references to the new filename.
3. Execute targeted tests/build governance checks.
4. Verify no remaining references to the old filename in tracked files.

Rollback: revert this change set in one commit if any critical downstream blocker appears before release.

## Open Questions

- None at implementation start; scope and hard-cut policy are explicit.
