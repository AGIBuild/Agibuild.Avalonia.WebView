## ADDED Requirements

### Requirement: Ci and CiPublish SHALL preserve closeout-critical governance parity
`Ci` and `CiPublish` target graphs MUST preserve parity for closeout-critical governance groups defined by transition invariants.  
When lane-specific targets differ by context, the mapping MUST be explicit and machine-checkable.

#### Scenario: Lane parity mapping is complete
- **WHEN** governance evaluates `Ci` and `CiPublish` dependency graphs
- **THEN** each required closeout-critical governance group is present in both lanes via direct target or invariant-defined mapping

#### Scenario: Unmapped lane divergence fails build governance
- **WHEN** a required closeout-critical governance target/group is missing from one lane without an approved mapping
- **THEN** governance fails deterministically before lane completion

### Requirement: Closeout-critical parity governance SHALL be regression-safe for future target additions
When new closeout-critical governance targets are introduced, build governance MUST require explicit inclusion in the lane parity invariant mapping.

#### Scenario: New target is added without parity mapping
- **WHEN** a new closeout-critical governance target is introduced in only one lane or without invariant mapping metadata
- **THEN** governance fails with actionable diagnostics identifying the missing parity mapping
