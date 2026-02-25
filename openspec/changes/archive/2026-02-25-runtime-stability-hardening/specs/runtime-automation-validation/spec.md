## ADDED Requirements

### Requirement: Runtime critical path SHALL include DevTools lifecycle cycle scenario
Runtime critical-path manifest SHALL include a dedicated shell scenario for DevTools lifecycle stability across repeated scope recreation.

#### Scenario: DevTools lifecycle cycle scenario is present
- **WHEN** governance reads runtime critical-path manifest
- **THEN** scenario id `shell-devtools-lifecycle-cycles` exists and maps to executable RuntimeAutomation evidence
