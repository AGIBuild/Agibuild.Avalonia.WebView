## ADDED Requirements

### Requirement: Bridge SHALL enforce reserved metadata registry before policy evaluation
The host capability bridge SHALL validate inbound metadata keys against a reserved registry and bounded extension lane before policy evaluation and dispatch callbacks.

#### Scenario: Registry violation skips policy and dispatch
- **WHEN** inbound metadata contains key outside reserved registry and extension lane
- **THEN** bridge returns deterministic deny, emits diagnostics, and policy evaluate call count remains zero

### Requirement: Bridge SHALL normalize inbound event timestamp for wire determinism
The host capability bridge SHALL normalize inbound `OccurredAtUtc` to UTC millisecond precision before dispatching typed events.

#### Scenario: Dispatch payload contains canonical timestamp
- **WHEN** inbound event passes validation with timestamp containing sub-millisecond precision
- **THEN** dispatched event payload exposes canonical UTC millisecond timestamp and diagnostics remain stable
