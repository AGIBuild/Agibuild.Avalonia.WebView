# blocking-wait-governance Specification

## Purpose
Define governance rules that restrict blocking waits in production and test/orchestration code, requiring explicit allowlists with rationale and bounded, condition-driven synchronization.
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

### Requirement: Build and orchestration blocking waits are allowlist-governed
Blocking waits used in build/test orchestration code (for example under `build/` and shared test orchestration helpers) MUST be governed by an explicit allowlist with owner and rationale metadata.

#### Scenario: New orchestration blocking wait is introduced
- **WHEN** governance checks scan orchestration code
- **THEN** any non-allowlisted blocking wait fails validation with file path and owner/rationale guidance

### Requirement: Synchronization waits are condition-driven and bounded
Polling or wait loops in tests and orchestration MUST be condition-driven with bounded timeout and diagnostic context.

#### Scenario: Wait loop has no explicit bound
- **WHEN** governance checks evaluate wait helper usage
- **THEN** the change fails until a bounded timeout and diagnostic context are provided

