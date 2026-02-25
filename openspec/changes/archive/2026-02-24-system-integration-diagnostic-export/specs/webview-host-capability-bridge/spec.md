## ADDED Requirements

### Requirement: Host capability diagnostics SHALL expose deterministic export protocol
The runtime SHALL provide a structured diagnostic export record with stable field mapping from host capability diagnostic events.

#### Scenario: Diagnostic event maps to export record
- **WHEN** runtime emits a host capability diagnostic event
- **THEN** consumer can convert it to an export record with deterministic schema version, operation, outcome, and context fields

### Requirement: Export protocol SHALL preserve deny/failure taxonomy semantics
Exported diagnostic records SHALL retain deny reason and failure category semantics for machine-readable regression.

#### Scenario: Deny and failure records keep taxonomy fields
- **WHEN** capability flow produces deny and failure outcomes
- **THEN** exported records include deny reason and failure category consistent with runtime diagnostics
