# blocking-wait-governance Specification

## Purpose
TBD - created by archiving change refactor-async-boundaries-and-test-stability. Update Purpose after archive.
## Requirements
### Requirement: Production blocking waits are allowlist-governed
Production source code under `src/` MUST keep `GetAwaiter().GetResult()` usage within an explicit allowlist enforced by automated tests.

#### Scenario: Non-allowlisted blocking wait is introduced
- **WHEN** a production file adds a new `GetAwaiter().GetResult()` call outside approved locations
- **THEN** the governance test fails with the file path and symbol context

### Requirement: Allowlist entries require deterministic justification
Each allowed blocking-wait location MUST include a deterministic reason tied to platform callback constraints or lifecycle boundaries.

#### Scenario: Existing allowlist entry has no reason
- **WHEN** governance metadata is validated
- **THEN** the change is rejected until rationale is present

### Requirement: Test code follows non-blocking default
Test projects MUST avoid direct blocking waits and MUST use centralized dispatcher/test pump helpers for synchronization.  
If a blocking wait is unavoidable in tests, it MUST be localized to approved synchronization helpers.

#### Scenario: Test introduces direct blocking wait in case body
- **WHEN** test governance checks run
- **THEN** the check flags the usage and points to centralized helper alternatives

