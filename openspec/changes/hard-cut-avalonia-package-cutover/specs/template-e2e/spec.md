## MODIFIED Requirements

### Requirement: TemplateE2E validates full packaging-to-test workflow
The `TemplateE2E` workflow SHALL cover end-to-end template verification from package generation through scaffold/build/test/cleanup, and SHALL assert host package identity wiring in generated projects.

#### Scenario: TemplateE2E completes with passing verification tests
- **WHEN** TemplateE2E automation executes
- **THEN** scaffolded project build and tests pass and cleanup completes deterministically

#### Scenario: TemplateE2E validates hard-cut host package identity
- **WHEN** TemplateE2E inspects generated desktop project dependencies
- **THEN** host dependency wiring references `Agibuild.Fulora.Avalonia`
- **AND** legacy package identity `Agibuild.Fulora` is absent from generated dependency declarations
