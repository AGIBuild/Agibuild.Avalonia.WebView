## ADDED Requirements

### Requirement: Bridge inbound metadata validation SHALL include aggregate budget stage
The host capability bridge SHALL validate inbound system-integration metadata with an explicit aggregate payload budget stage before policy evaluation and dispatch callbacks.

#### Scenario: Aggregate budget deny bypasses policy/provider execution
- **WHEN** inbound metadata exceeds aggregate budget at boundary validation stage
- **THEN** bridge returns deterministic deny diagnostics and does not invoke policy handler or dispatch subscribers

### Requirement: Boundary diagnostics SHALL identify aggregate budget outcome
Inbound bridge diagnostics SHALL include deterministic deny reason metadata that distinguishes aggregate-budget rejection from other envelope constraints.

#### Scenario: Aggregate budget rejection emits stable reason
- **WHEN** two equivalent over-budget inbound payloads are submitted
- **THEN** diagnostics expose the same boundary-stage deny reason and operation metadata across runs
