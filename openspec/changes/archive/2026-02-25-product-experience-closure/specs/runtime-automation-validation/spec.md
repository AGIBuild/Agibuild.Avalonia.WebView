## ADDED Requirements

### Requirement: Runtime critical path SHALL include product-experience closure scenario
Runtime critical-path manifest SHALL include a product-level shell scenario that validates file/menu capability behavior across permission deny/recover transitions.

#### Scenario: Product-experience closure scenario is listed with executable evidence
- **WHEN** governance reads `runtime-critical-path.manifest.json`
- **THEN** scenario id `shell-product-experience-closure` exists and maps to executable RuntimeAutomation evidence
