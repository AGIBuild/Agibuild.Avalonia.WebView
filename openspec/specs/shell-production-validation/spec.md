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
The repository SHALL include a machine-readable shell production matrix artifact that declares shell capabilities, platform coverage, and executable evidence mappings, including release-critical shell domains (soak/lifecycle, host capability, DevTools policy governance, shortcut routing governance).

#### Scenario: Matrix entries include platform and evidence metadata
- **WHEN** the shell production matrix is parsed by governance tests
- **THEN** each declared capability includes explicit coverage entries for Windows, macOS, and Linux and references at least one executable evidence item

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

