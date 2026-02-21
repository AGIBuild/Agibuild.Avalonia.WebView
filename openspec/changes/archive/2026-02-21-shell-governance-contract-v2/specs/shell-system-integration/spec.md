## ADDED Requirements

### Requirement: Inbound metadata aggregate budget SHALL be host-configurable within safe bounds
The system SHALL allow host configuration of inbound metadata aggregate budget with deterministic lower/upper bounds and SHALL enforce deny semantics when configured or effective budget is exceeded.

#### Scenario: Default budget is applied when host does not configure override
- **WHEN** inbound system-integration metadata is evaluated and no budget override is configured
- **THEN** runtime uses default aggregate budget and keeps deterministic allow/deny behavior

#### Scenario: Budget outside allowed bounds is rejected deterministically
- **WHEN** host configures aggregate metadata budget outside declared min/max bounds
- **THEN** runtime rejects the configuration deterministically and does not run with invalid boundary settings
