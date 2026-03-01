## Purpose
Define canonical repository path identity for the Avalonia host layer so directory naming remains deterministic and aligned with package/project identity.

## Requirements

### Requirement: Avalonia host directory SHALL use canonical identity path
The repository SHALL place the Avalonia host project under `src/Agibuild.Fulora.Avalonia` so directory-level identity matches the canonical package/project identity.

#### Scenario: Canonical host directory is used
- **WHEN** host project paths are resolved from repository-controlled configuration
- **THEN** the resolved path uses `src/Agibuild.Fulora.Avalonia`

### Requirement: Governed references MUST NOT use legacy host directory path
Governed build/test/docs/solution references MUST NOT keep `src/Agibuild.Fulora` path tokens for the Avalonia host project after canonicalization.

#### Scenario: Legacy host directory reference remains
- **WHEN** any governed file references `src/Agibuild.Fulora` for Avalonia host artifacts
- **THEN** validation fails until the reference is migrated to `src/Agibuild.Fulora.Avalonia`
