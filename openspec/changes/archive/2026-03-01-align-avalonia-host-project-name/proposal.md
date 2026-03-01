## Why

The Avalonia host package was intentionally renamed to `Agibuild.Fulora.Avalonia`, but the host project file still uses `Agibuild.Fulora.csproj`. This creates avoidable identity drift in build scripts, diagnostics, and maintenance workflows where project identity is inferred from file names.

Aligning project-file identity with package identity now keeps release metadata deterministic and reduces naming ambiguity while Phase 8 governance and packaging hardening are active.

## What Changes

- Rename the host project file from `src/Agibuild.Fulora/Agibuild.Fulora.csproj` to `src/Agibuild.Fulora/Agibuild.Fulora.Avalonia.csproj`.
- Update all repository references that point to the old project file name (solution files, build scripts, tests, templates, and CI-facing paths).
- Keep runtime/package behavior unchanged; this is an identity-alignment refactor.
- Add/adjust tests/governance assertions where path-based project identity is validated.

## Capabilities

### New Capabilities
- `avalonia-host-project-identity-alignment`: Enforce deterministic naming alignment between the Avalonia host project file and its canonical package identity.

### Modified Capabilities
- None.

## Impact

- Affected code: project references, packaging/test/governance path assertions, and any hard-coded `.csproj` paths.
- APIs/runtime behavior: no protocol/API change.
- Dependencies/systems: build and CI path resolution for host packaging.
- Goal and roadmap alignment: supports **G4 (Contract-Driven Testability)** by removing path ambiguity in governed checks; aligns with Phase 8 stabilization work that emphasizes deterministic release and governance behavior.

## Non-goals

- No compatibility shims, transitional metapackages, or deprecation workflows.
- No package ID changes.
- No functional runtime feature changes in bridge/shell/spa hosting.
