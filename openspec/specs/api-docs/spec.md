## Purpose
Define API documentation governance for reference generation and user-facing guides.

## Requirements

### Requirement: XML documentation generation is enabled for product assemblies
The build system SHALL enable XML documentation generation for product assemblies and SHALL disable it for tests and benchmarks.

#### Scenario: XML docs are generated for product projects
- **WHEN** product projects are built
- **THEN** XML documentation files are emitted and included in doc metadata inputs

### Requirement: docfx configuration is repository-governed
The repository SHALL include docfx configuration for the core runtime/public surface and SHALL publish site output to a deterministic `_site` directory.

#### Scenario: docfx metadata and site output are deterministic
- **WHEN** docfx build runs in CI or local automation
- **THEN** metadata is produced from project files and site content is generated under `_site`

### Requirement: Getting Started guide is maintained
The documentation set SHALL include a Getting Started guide covering prerequisites, template quick start, manual setup, and navigation basics.

#### Scenario: Getting Started guide covers onboarding path
- **WHEN** a new contributor follows the Getting Started article
- **THEN** they can complete setup and run a baseline sample flow

### Requirement: Topic guides are maintained for core workflows
The documentation set SHALL include dedicated topic guides for bridge usage, SPA hosting, and architecture overview.

#### Scenario: Topic guides are discoverable from docs index
- **WHEN** a user browses documentation topics
- **THEN** bridge, SPA hosting, and architecture guides are available and linked
