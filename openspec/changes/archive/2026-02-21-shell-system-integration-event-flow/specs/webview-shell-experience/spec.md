## ADDED Requirements

### Requirement: Shell experience SHALL govern bidirectional system integration flows
The shell experience component SHALL provide deterministic, policy-governed entry points for outbound system integration commands and inbound host-originated system integration events.

#### Scenario: Outbound command and inbound event both route through shell governance
- **WHEN** app issues system integration command and host emits corresponding interaction event
- **THEN** both directions are processed through shell governance with deterministic outcomes

### Requirement: Dynamic menu pruning SHALL execute in shell governance pipeline
Shell experience SHALL evaluate menu pruning policy within its deterministic policy pipeline before effective menu state is applied.

#### Scenario: Menu pruning deny prevents state mutation
- **WHEN** menu pruning policy denies effective menu state update
- **THEN** runtime does not mutate current effective menu state and emits explicit policy failure metadata

### Requirement: Inbound system integration failures SHALL be isolated from other shell domains
Inbound system integration deny/failure SHALL NOT break permission/download/new-window/devtools/command domains.

#### Scenario: Inbound event failure does not break permission governance
- **WHEN** inbound system integration event handling fails
- **THEN** subsequent permission requests continue to be processed deterministically according to configured policies
