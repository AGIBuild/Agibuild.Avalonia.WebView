## ADDED Requirements

### Requirement: Test harness enforces lane taxonomy and ownership
The test harness SHALL enforce a lane taxonomy that distinguishes `ContractAutomation` from `RuntimeAutomation`, with each test suite declaring lane ownership and execution intent.

#### Scenario: New automation test is added
- **WHEN** a new test class is introduced
- **THEN** it is placed in a lane-specific suite and mapped to the corresponding CI target

### Requirement: Runtime lane coverage targets are explicit
The harness MUST maintain a runtime critical-path manifest that maps each required runtime scenario to owning test cases.

#### Scenario: Runtime coverage manifest is reviewed
- **WHEN** release-readiness validation runs
- **THEN** each required runtime scenario has at least one passing mapped test case

### Requirement: Lifecycle wiring assertions avoid reflection-only seams
Lifecycle/event wiring assertions in automation tests SHALL prefer dedicated internal test hooks or facades over broad private reflection.

#### Scenario: Lifecycle assertion requires core reattach verification
- **WHEN** a test validates event wiring after detach/reattach
- **THEN** it uses approved hook/facade points and does not rely solely on arbitrary private-member reflection
