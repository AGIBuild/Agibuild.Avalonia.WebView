## Context

`build/Build.cs` currently contains entry bootstrap, target graph, warning governance, platform helpers, publishing retries, and react/npm utility logic in one file.
This slows iteration and makes ownership reviews noisy.

## Goals / Non-goals

**Goals**
- Introduce explicit responsibility boundaries using `partial class BuildTask`.
- Keep Nuke entry and target behavior fully compatible.
- Preserve existing CI gate semantics and artifacts.

**Non-goals**
- No functional target contract change.
- No command signature change for scripts/workflows.
- No feature additions in build pipeline behavior.

## Decisions

### Decision 1: Rename entry class to `BuildTask`
- Replace `_Build` with `BuildTask`.
- Update `Main` bootstrap to `Execute<BuildTask>(x => x.Build)`.
- Keep target name `Build` as-is.

### Decision 2: Split by cohesive responsibility clusters
- Keep `build/Build.cs` as entry + parameters + paths + target declarations.
- Move warning governance records/methods into `Build.WarningGovernance.cs`.
- Move general process and parsing helpers into `Build.Helpers.cs`.
- Move NuGet/package smoke helpers into `Build.Publishing.cs`.
- Move react/npm helper methods into `Build.React.cs`.

### Decision 3: Preserve contracts with governance assertions
- Update governance tests to assert class entry and target contracts without over-coupling to one file layout.
- Verify key markers still exist for closeout snapshot and strict governance gates.

## Risks / Trade-offs

- [Risk] Entry class rename causes Nuke entry resolution failure.  
  - Mitigation: immediate `nuke Test`/`nuke Coverage` validation after rename.

- [Risk] String-based governance assertions become brittle after split.  
  - Mitigation: adjust assertions to contract-level markers and target graph invariants.

## Testing Strategy

- `openspec validate --all --strict`
- `nuke Test`
- `nuke Coverage`
- Optional: `nuke Ci` to validate full dependency chain after split.
