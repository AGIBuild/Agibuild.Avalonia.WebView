## ADDED Requirements

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
