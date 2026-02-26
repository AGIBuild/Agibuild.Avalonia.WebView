## ADDED Requirements

### Requirement: CI governance SHALL enforce runtime critical-path execution evidence
Build pipeline governance SHALL include a deterministic gate that validates runtime critical-path execution evidence from TRX artifacts.

#### Scenario: CI fails when critical-path execution evidence is incomplete
- **WHEN** CI executes governance targets and required critical-path evidence is missing or failed
- **THEN** pipeline fails with machine-readable failure reasons
