## ADDED Requirements

### Requirement: Shell inbound flow SHALL preserve canonicalized event semantics
Shell experience SHALL relay inbound system-integration events using canonicalized boundary data produced by host capability bridge.

#### Scenario: Shell subscriber receives canonical timestamp
- **WHEN** shell publishes inbound event through bridge with sub-millisecond UTC timestamp
- **THEN** shell event subscriber receives canonical UTC millisecond timestamp deterministically

### Requirement: Shell failure isolation SHALL hold for reserved-key violations
Inbound reserved-key validation failures SHALL remain isolated from permission/download/new-window governance flows.

#### Scenario: Reserved-key deny does not break permission/download/new-window
- **WHEN** inbound event is denied due to reserved-key registry violation
- **THEN** permission/download/new-window domains continue deterministic behavior for subsequent operations
