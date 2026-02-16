## ADDED Requirements

### Requirement: WebViewCore hotspot branch coverage is explicitly governed
The test harness SHALL maintain a machine-readable hotspot manifest for low-covered `WebViewCore` branch paths that are considered release-critical.
Each hotspot entry SHALL map to at least one executable test owner in an automation lane.

#### Scenario: Hotspot manifest references missing evidence
- **WHEN** a hotspot entry cannot be resolved to an existing executable test
- **THEN** governance validation fails the test harness gate

### Requirement: Hotspot branch evidence is deterministic and lane-scoped
For each declared `WebViewCore` hotspot, the owning test evidence MUST run deterministically in the declared lane (`ContractAutomation` or `RuntimeAutomation`) and SHALL be re-runnable without timing sleeps.

#### Scenario: Hotspot test uses non-deterministic waiting
- **WHEN** a hotspot-owner test relies on unbounded or sleep-based waiting
- **THEN** harness governance marks the evidence invalid until deterministic synchronization is restored

### Requirement: Coverage closure for hotspots is auditable
Coverage verification SHALL include an auditable mapping between `WebViewCore` hotspot branches and test execution results in CI artifacts.

#### Scenario: Coverage target improves but hotspot mapping is absent
- **WHEN** aggregate coverage increases but hotspot-to-test traceability is missing
- **THEN** the quality gate fails due to missing audit evidence
