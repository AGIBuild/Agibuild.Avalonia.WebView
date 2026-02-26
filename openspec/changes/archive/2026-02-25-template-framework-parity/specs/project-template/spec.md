## ADDED Requirements

### Requirement: Framework selection SHALL materialize framework-specific web scaffold
Template framework selection SHALL generate concrete framework-specific web scaffold content for `react` and `vue` choices.

#### Scenario: React framework emits React Vite scaffold
- **WHEN** template is instantiated with `--framework react`
- **THEN** generated project contains React Vite web scaffold with buildable `package.json` scripts

#### Scenario: Vue framework emits Vue Vite scaffold
- **WHEN** template is instantiated with `--framework vue`
- **THEN** generated project contains Vue Vite web scaffold with buildable `package.json` scripts
