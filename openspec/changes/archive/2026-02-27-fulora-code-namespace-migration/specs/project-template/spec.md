## MODIFIED Requirements

### Requirement: Template metadata SHALL be well-defined
The template SHALL define stable identity and classification metadata in `template.json` so it can be discovered and invoked consistently, including explicit shell preset metadata.

#### Scenario: Template identity and short name are present
- **WHEN** template metadata is inspected
- **THEN** identity is `Agibuild.Fulora.HybridTemplate` and short name is `agibuild-fulora-hybrid`

#### Scenario: Template classifications and directory preference are configured
- **WHEN** template metadata is inspected
- **THEN** classifications include Desktop, Mobile, Hybrid, Avalonia, WebView and `PreferNameDirectory` is enabled

#### Scenario: Shell preset symbol metadata is defined
- **WHEN** template metadata is inspected
- **THEN** shell preset symbol is present with explicit choices and default value
