# shell-production-validation Specification

## Purpose
Define deterministic shell production-readiness validation requirements for long-run soak workloads, platform coverage tracking, and auditable release-critical evidence.
## Requirements
### Requirement: Shell production soak validation is deterministic and repeatable
The system SHALL provide deterministic long-run shell soak validation that exercises repeated shell-scope attach/detach cycles with multi-window and policy/capability orchestration, including deterministic policy/capability cleanup between cycles.

#### Scenario: Repeated shell-scope attach/detach cycles preserve cleanup invariants
- **WHEN** runtime automation executes the shell production soak test workload
- **THEN** each cycle completes without stale managed windows, leaked policy handlers, or residual shell state

### Requirement: Shell production matrix is machine-readable and auditable
The repository SHALL include a machine-readable shell production matrix artifact that declares shell capabilities, platform coverage, and executable evidence mappings, and each evidence mapping MUST be consumable by semantic governance assertions and evidence-contract v2 provenance rules.

#### Scenario: Matrix entries include platform and evidence metadata
- **WHEN** the shell production matrix is parsed by governance tests
- **THEN** each declared capability includes explicit coverage entries for Windows, macOS, and Linux and references at least one executable evidence item with required semantic/provenance metadata

### Requirement: Release-critical shell soak evidence is tracked in runtime critical path
Shell production soak and governance-critical shell scenarios SHALL be listed in runtime critical-path governance so regressions fail release-readiness checks early.

#### Scenario: Missing shell soak critical-path scenario fails governance
- **WHEN** required shell soak/governance scenario IDs are missing from the runtime critical-path manifest
- **THEN** governance validation fails with deterministic diagnostics

### Requirement: Shell production matrix SHALL include diagnostic export capability evidence
Shell production validation matrix SHALL include a capability row for system-integration diagnostic export protocol with executable evidence mapping.

#### Scenario: Production matrix includes diagnostic export capability
- **WHEN** governance validates shell production matrix
- **THEN** diagnostic export capability id exists with platform coverage and integration evidence

### Requirement: Phase closeout governance SHALL detect diagnostic and template regression entry points
Shell production closeout governance SHALL assert that diagnostic export and template regression entry points remain detectable in repository sources.

#### Scenario: Entry-point marker removal fails governance
- **WHEN** diagnostic export marker or template regression entry function marker is removed
- **THEN** governance tests fail with deterministic signal

### Requirement: Shell production matrix SHALL include product-experience closure capability
Shell production matrix SHALL include a dedicated capability row for product-experience closure with explicit platform coverage and executable evidence.

#### Scenario: Product-experience closure capability is declared and traceable
- **WHEN** governance validates `shell-production-matrix.json`
- **THEN** capability id `shell-product-experience-closure` exists with per-platform coverage and RuntimeAutomation evidence mapping

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

### Requirement: Shell production matrix SHALL include DevTools lifecycle stability capability
Shell production matrix SHALL include a dedicated capability for DevTools lifecycle cycle stability with executable RuntimeAutomation evidence.

#### Scenario: DevTools lifecycle capability is declared in production matrix
- **WHEN** governance validates shell production matrix
- **THEN** capability id `shell-devtools-lifecycle-cycles` exists with platform coverage and evidence mapping

### Requirement: Shell manifest-matrix consistency SHALL be bidirectional
Shell governance SHALL enforce bidirectional consistency between runtime critical-path shell scenarios and shell production matrix capability IDs using a shared semantic invariant source, and mismatches MUST fail closeout validation deterministically.

#### Scenario: Matrix-only shell capability ID fails governance
- **WHEN** a shell capability exists in production matrix but has no runtime critical-path scenario mapping
- **THEN** governance fails before closeout validation with deterministic invariant diagnostics

