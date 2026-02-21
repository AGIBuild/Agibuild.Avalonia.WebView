## ADDED Requirements

### Requirement: Bridge metadata budget validation SHALL be deterministic and bounded
The host capability bridge SHALL apply configurable aggregate metadata budget validation using deterministic min/max bounded options before policy evaluation and dispatch.

#### Scenario: Over-budget payload is denied with stable reason using effective configured budget
- **WHEN** inbound metadata total length exceeds effective configured aggregate budget
- **THEN** bridge returns deterministic deny reason and skips policy/provider/dispatch execution

#### Scenario: In-range configured budget drives validation branch deterministically
- **WHEN** host configures an in-range aggregate budget value
- **THEN** bridge uses that value consistently for equivalent inbound payload evaluations
