## MODIFIED Requirements

### Requirement: docfx configuration is repository-governed
The repository SHALL include docfx configuration for the core runtime/public surface and SHALL publish site output to a deterministic `_site` directory.
The generated API documentation site SHALL use `Agibuild.Fulora` as canonical product identity in top-level site metadata and navigation labels.

#### Scenario: docfx metadata and site output are deterministic
- **WHEN** docfx build runs in CI or local automation
- **THEN** metadata is produced from project files and site content is generated under `_site`

#### Scenario: Site identity is canonical
- **WHEN** documentation site metadata is generated
- **THEN** top-level product title and navigation identity use `Agibuild.Fulora`
