## ADDED Requirements

### Requirement: Federated pruning diagnostics SHALL include profile revision attribution
Shell federated pruning diagnostics SHALL carry profile revision attribution fields (`profileVersion`, `profileHash`) when profile resolver provides them.

#### Scenario: Pruning diagnostics include profile identity and revision fields
- **WHEN** profile-governed pruning evaluation runs with profile revision metadata available
- **THEN** diagnostics include profile identity, profile version/hash, stage attribution, and final deterministic outcome

#### Scenario: Missing revision metadata remains deterministic
- **WHEN** profile resolver omits profile version/hash
- **THEN** diagnostics remain valid with stable null/empty semantics and unchanged pruning decision behavior
