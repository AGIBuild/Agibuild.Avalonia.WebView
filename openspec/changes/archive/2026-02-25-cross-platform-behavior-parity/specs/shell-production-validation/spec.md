## ADDED Requirements

### Requirement: Shell production matrix SHALL declare five-platform parity envelope
Shell production matrix SHALL declare explicit platform keys for `windows`, `macos`, `linux`, `ios`, and `android` across every capability coverage entry.

#### Scenario: Every capability contains five-platform coverage keys
- **WHEN** governance parses shell production matrix
- **THEN** each capability has non-empty coverage arrays for all five platform keys

### Requirement: Shell production coverage tokens SHALL be governed
Coverage tokens in shell production matrix SHALL use controlled vocabulary (`ct`, `it-smoke`, `it-soak`, `n/a`).

#### Scenario: Unsupported token fails governance
- **WHEN** matrix coverage includes a token outside the controlled vocabulary
- **THEN** governance validation fails with deterministic diagnostics
