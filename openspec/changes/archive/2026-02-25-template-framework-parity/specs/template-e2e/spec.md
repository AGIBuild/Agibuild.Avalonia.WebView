## ADDED Requirements

### Requirement: TemplateE2E SHALL validate framework-specific web build paths
TemplateE2E workflow SHALL validate generated React and Vue web scaffold build paths in addition to baseline .NET build/test flow.

#### Scenario: React scaffold web build succeeds in TemplateE2E
- **WHEN** TemplateE2E runs against react framework template output
- **THEN** `npm install` and `npm run build` succeed for generated React web project

#### Scenario: Vue scaffold web build succeeds in TemplateE2E
- **WHEN** TemplateE2E runs against vue framework template output
- **THEN** `npm install` and `npm run build` succeed for generated Vue web project
