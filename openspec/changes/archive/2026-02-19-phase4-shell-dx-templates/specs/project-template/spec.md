## MODIFIED Requirements

### Requirement: Template metadata SHALL be well-defined
The template SHALL define stable identity and classification metadata in `template.json` so it can be discovered and invoked consistently, including explicit shell preset metadata.

#### Scenario: Template identity and short name are present
- **WHEN** template metadata is inspected
- **THEN** identity is `Agibuild.Fulora.HybridTemplate` and short name is `agibuild-hybrid`

#### Scenario: Template classifications and directory preference are configured
- **WHEN** template metadata is inspected
- **THEN** classifications include Desktop, Mobile, Hybrid, Avalonia, WebView and `PreferNameDirectory` is enabled

#### Scenario: Shell preset symbol metadata is defined
- **WHEN** template metadata is inspected
- **THEN** shell preset symbol is present with explicit choices and default value

### Requirement: Template SHALL support framework selection
The template SHALL expose framework and shell preset choice parameters with supported values and generate conditional source content based on selected options.

#### Scenario: Framework choice drives scaffolded content
- **WHEN** the framework parameter is set to one of `vanilla`, `react`, or `vue`
- **THEN** generated source files match the selected framework

#### Scenario: Shell preset choice drives desktop host wiring
- **WHEN** shell preset parameter is set to `baseline` or `app-shell`
- **THEN** generated desktop source contains the corresponding preset-specific shell wiring path
