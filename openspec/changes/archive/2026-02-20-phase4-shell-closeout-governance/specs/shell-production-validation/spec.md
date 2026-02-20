## MODIFIED Requirements

### Requirement: Shell production matrix is machine-readable and auditable
The repository SHALL include a machine-readable shell production matrix artifact that declares shell capabilities, platform coverage, and executable evidence mappings, including release-critical shell domains (soak/lifecycle, host capability, DevTools policy governance, shortcut routing governance).

#### Scenario: Matrix entries include platform and evidence metadata
- **WHEN** the shell production matrix is parsed by governance tests
- **THEN** each declared capability includes explicit coverage entries for Windows, macOS, and Linux and references at least one executable evidence item

### Requirement: Release-critical shell soak evidence is tracked in runtime critical path
Shell production soak and governance-critical shell scenarios SHALL be listed in runtime critical-path governance so regressions fail release-readiness checks early.

#### Scenario: Missing shell soak critical-path scenario fails governance
- **WHEN** required shell soak/governance scenario IDs are missing from the runtime critical-path manifest
- **THEN** governance validation fails with deterministic diagnostics
