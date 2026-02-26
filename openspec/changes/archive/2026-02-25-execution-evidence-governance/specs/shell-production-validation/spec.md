## ADDED Requirements

### Requirement: Shell manifest-matrix consistency SHALL be bidirectional
Shell governance SHALL enforce bidirectional consistency between runtime critical-path shell scenarios and shell production matrix capability IDs.

#### Scenario: Matrix-only shell capability ID fails governance
- **WHEN** a shell capability exists in production matrix but has no runtime critical-path scenario mapping
- **THEN** governance fails before closeout validation
