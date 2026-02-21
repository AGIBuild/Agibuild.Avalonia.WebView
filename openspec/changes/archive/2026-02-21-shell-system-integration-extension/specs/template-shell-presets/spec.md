## MODIFIED Requirements

### Requirement: App-shell preset scaffolds shell-ready desktop startup
When app-shell preset is selected, the generated desktop host SHALL include shell startup wiring that consumes existing runtime shell contracts and includes typed system integration wiring for menu/tray baseline flows.

#### Scenario: App-shell preset emits shell experience bootstrap code
- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop startup source includes shell experience initialization and deterministic disposal hooks

#### Scenario: App-shell preset emits system integration bootstrap markers
- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated source contains typed system integration registration markers for menu/tray capability flow

## ADDED Requirements

### Requirement: App-shell preset SHALL demonstrate Web-first system integration flow
The app-shell preset SHALL demonstrate a canonical flow from web call to typed bridge, capability gateway, policy evaluation, and typed system integration result.

#### Scenario: Template includes menu or tray typed service exposure
- **WHEN** app-shell preset output is generated
- **THEN** desktop host exposes a typed bridge service that routes system integration operations through shell capability governance

#### Scenario: Governance tests validate system integration preset markers
- **WHEN** repository governance tests run
- **THEN** tests deterministically assert presence of system integration preset markers and absence of direct platform API bypass markers
