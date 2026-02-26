## MODIFIED Requirements

### Requirement: Shell production matrix is machine-readable and auditable
The repository SHALL include a machine-readable shell production matrix artifact that declares shell capabilities, platform coverage, and executable evidence mappings, and each evidence mapping MUST be consumable by semantic governance assertions and evidence-contract v2 provenance rules.

#### Scenario: Matrix entries include platform and evidence metadata
- **WHEN** the shell production matrix is parsed by governance tests
- **THEN** each declared capability includes explicit coverage entries for Windows, macOS, and Linux and references at least one executable evidence item with required semantic/provenance metadata

### Requirement: Shell manifest-matrix consistency SHALL be bidirectional
Shell governance SHALL enforce bidirectional consistency between runtime critical-path shell scenarios and shell production matrix capability IDs using a shared semantic invariant source, and mismatches MUST fail closeout validation deterministically.

#### Scenario: Matrix-only shell capability ID fails governance
- **WHEN** a shell capability exists in production matrix but has no runtime critical-path scenario mapping
- **THEN** governance fails before closeout validation with deterministic invariant diagnostics
