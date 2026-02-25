## ADDED Requirements

### Requirement: DevTools policy lifecycle SHALL remain stable across shell scope recreation
Shell experience SHALL preserve deterministic DevTools policy behavior across repeated shell scope create/dispose cycles.

#### Scenario: Recreated shell scopes keep deterministic DevTools outcomes
- **WHEN** runtime repeatedly creates and disposes shell scopes with alternating DevTools policy decisions
- **THEN** deny/allow outcomes remain deterministic and do not leak behavior between cycles
