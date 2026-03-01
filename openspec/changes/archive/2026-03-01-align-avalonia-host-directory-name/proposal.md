## Why

After aligning the host project file name to `Agibuild.Fulora.Avalonia.csproj`, the containing directory still uses `src/Agibuild.Fulora`. This keeps path-level identity inconsistent and increases governance/build maintenance complexity for path-based checks.

Renaming the directory now removes remaining identity drift and keeps repository structure deterministic during ongoing release-governance hardening.

## What Changes

- Rename host directory from `src/Agibuild.Fulora` to `src/Agibuild.Fulora.Avalonia`.
- Update all repository references that currently point to the old host directory path.
- Keep package/runtime behavior unchanged; this is a structural identity alignment.
- Add/adjust governance tests for the canonical host directory path.

## Capabilities

### New Capabilities
- `avalonia-host-directory-identity-alignment`: Enforce canonical path identity for the Avalonia host project directory.

### Modified Capabilities
- None.

## Impact

- Affected code: solution entries, build scripts, test path assertions, docs source paths, and project references.
- APIs/runtime behavior: no API/protocol/feature change.
- Dependencies/systems: CI/build path resolution and governance checks.
- Goal and roadmap alignment: supports **G4** by reducing path ambiguity in test/governance automation, and supports **E1** template/build determinism in current stabilization work.

## Non-goals

- No compatibility aliases for old directory paths.
- No package ID, assembly contract, or namespace behavior changes.
- No bridge/shell/runtime feature work.
