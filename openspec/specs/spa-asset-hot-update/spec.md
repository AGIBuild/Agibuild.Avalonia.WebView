## Purpose
Define deterministic signed SPA asset package hot-update, activation, and rollback behavior for production hosting workflows.

## Requirements

### Requirement: SPA update package installation SHALL verify signature before activation
SPA hot-update workflow MUST validate package signature and digest before any active version switch.

#### Scenario: Valid signed package installs successfully
- **WHEN** package digest matches payload and signature validation passes
- **THEN** package is extracted into versioned staging location
- **AND** activation may proceed

#### Scenario: Invalid signature blocks installation
- **WHEN** signature validation fails
- **THEN** package installation is rejected deterministically
- **AND** active version remains unchanged

### Requirement: Activation switch SHALL be atomic and rollback-capable
Version activation MUST update runtime active pointer atomically and support deterministic rollback to previous version.

#### Scenario: Activation succeeds and previous version is retained for rollback
- **WHEN** new version activation succeeds
- **THEN** active pointer switches to new version atomically
- **AND** previous version metadata is retained for rollback

#### Scenario: Rollback restores prior active version
- **WHEN** rollback is requested after successful activation
- **THEN** active pointer restores previous version deterministically
- **AND** serving path resolves to restored version
