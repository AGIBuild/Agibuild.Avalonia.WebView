## ADDED Requirements

### Requirement: Profile revision diagnostics SHALL use canonical normalized semantics
Shell profile diagnostics SHALL normalize revision fields so `profileVersion` is trimmed-or-null and `profileHash` follows canonical `sha256:<64-lower-hex>` or null when invalid.

#### Scenario: Valid profile revision fields are propagated canonically
- **WHEN** profile resolver provides valid version/hash fields
- **THEN** diagnostics expose normalized canonical revision metadata with deterministic field values

#### Scenario: Invalid profile hash is normalized to null without affecting policy outcome
- **WHEN** profile resolver provides invalid hash format
- **THEN** diagnostics emit null hash field and pruning/permission decision behavior remains unchanged
