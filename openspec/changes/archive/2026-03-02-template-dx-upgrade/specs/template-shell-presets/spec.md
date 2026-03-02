## MODIFIED Requirements

### Requirement: App-shell preset scaffolds shell-ready desktop startup

When app-shell preset is selected, the generated desktop host SHALL include shell startup wiring that consumes existing runtime shell contracts and includes typed system integration wiring for menu/tray baseline flows, theme synchronization, and `AvaloniaHostCapabilityProvider` binding.

#### Scenario: App-shell preset emits shell experience bootstrap code

- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop startup source includes shell experience initialization and deterministic disposal hooks

#### Scenario: App-shell preset emits system integration bootstrap markers

- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated source contains typed system integration registration markers for menu/tray capability flow

#### Scenario: App-shell preset wires AvaloniaHostCapabilityProvider

- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop startup source SHALL create and wire `AvaloniaHostCapabilityProvider` as the capability provider
- **AND** tray icon and native menu SHALL be configured with default values

#### Scenario: App-shell preset registers IThemeService

- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop startup source SHALL register `IThemeService` via `Bridge.Expose<IThemeService>()`
- **AND** the Web frontend SHALL include theme-aware styling that reacts to `ThemeChangedEvent`

#### Scenario: App-shell preset configures tray icon with default app icon

- **WHEN** user runs `dotnet new agibuild-hybrid` with shell preset `app-shell`
- **THEN** generated desktop startup source SHALL configure tray icon with the application icon, app name tooltip, and visible state

### Requirement: App-shell preset Web frontend demonstrates shell capabilities

When app-shell preset is selected, the generated Web frontend SHALL include UI components that demonstrate tray, menu, and theme bridge interactions.

#### Scenario: App-shell React frontend includes theme toggle

- **WHEN** user runs `dotnet new agibuild-hybrid --framework react` with shell preset `app-shell`
- **THEN** generated React app SHALL include a component that displays current OS theme and reacts to theme change events

#### Scenario: App-shell React frontend includes menu control

- **WHEN** user runs `dotnet new agibuild-hybrid --framework react` with shell preset `app-shell`
- **THEN** generated React app SHALL include a component that applies a menu model to the native menu via bridge
