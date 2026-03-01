## Why

The runtime/core layers are already host-framework-neutral, but the public package identity still centers on `Agibuild.Fulora`, which obscures the Avalonia-specific host boundary and causes dependency ambiguity for consumers. We need an explicit hard cut to `Agibuild.Fulora.Avalonia` now to make host-layer ownership deterministic before the next release train.

## What Changes

- **BREAKING**: Replace the primary Avalonia host package identity from `Agibuild.Fulora` to `Agibuild.Fulora.Avalonia`.
- **BREAKING**: Remove compatibility/transition package behavior; no metapackage and no compatibility forwarding.
- Update build/release governance to validate the new canonical primary distributable package identity.
- Update template/sample/NuGet-package-consumer references to the new package identity.
- Keep core/runtime/adapter package identities and host-neutral contracts unchanged.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `release-versioning-strategy`: stable release identity requirements must target `Agibuild.Fulora.Avalonia` as the primary host package.
- `release-distribution-determinism`: canonical package-set checks must require the new primary host package ID.
- `project-template`: generated desktop host dependencies must reference the Avalonia host package identity explicitly (`Agibuild.Fulora.Avalonia`).
- `template-e2e`: template install/run validation must assert the new package identity path.

## Impact

- Build and packaging governance in `build/Build.cs`, `build/Build.Packaging.cs`, `build/Build.Governance.cs`, and helper assertions.
- Host package project identity in `src/Agibuild.Fulora/*` (project/package naming and packaging metadata).
- Template/sample/package-consumer dependency wiring under `templates/*`, `samples/*`, and integration NuGet tests.
- Contract/governance tests that assert package naming and canonical package set.
- No behavioral/runtime contract change for bridge, shell, SPA hosting, or adapter execution semantics.

## Non-goals

- No backward-compatibility layer, alias package, or transitional metapackage.
- No deprecation/migration communication workflow in this change.
- No redesign of runtime architecture, bridge contracts, or platform adapter SPI.
