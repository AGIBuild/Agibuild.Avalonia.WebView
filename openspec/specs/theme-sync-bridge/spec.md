## Purpose

Define requirements for the OS theme synchronization bridge service.

## Requirements

### Requirement: IThemeService exposes current OS theme information via typed bridge

The system SHALL provide an `IThemeService` interface decorated with `[JsExport]` that exposes methods for querying the current OS theme state.

#### Scenario: GetCurrentTheme returns theme info with mode

- **WHEN** JS calls `themeService.getCurrentTheme()`
- **THEN** the bridge SHALL return a `ThemeInfo` object with `mode` set to `"light"`, `"dark"`, or `"system"`

#### Scenario: GetAccentColor returns hex color string or null

- **WHEN** JS calls `themeService.getAccentColor()`
- **THEN** the bridge SHALL return a hex color string (`"#RRGGBB"`) on platforms that support accent color, or `null` on unsupported platforms

#### Scenario: GetHighContrastMode returns boolean

- **WHEN** JS calls `themeService.getHighContrastMode()`
- **THEN** the bridge SHALL return `true` if the OS is in high-contrast mode, `false` otherwise

### Requirement: Theme changes are pushed to JS via BridgeEvent

The `IThemeService` SHALL expose an `IBridgeEvent<ThemeChangedEvent>` property that fires when the OS theme changes.

#### Scenario: OS theme change from light to dark fires ThemeChangedEvent

- **WHEN** the OS theme changes from light to dark mode
- **THEN** the bridge SHALL push a `ThemeChangedEvent` to JS containing `currentTheme` with `mode = "dark"` and `previousMode = "light"`

#### Scenario: ThemeChangedEvent includes accent color if available

- **WHEN** the OS theme changes and the platform supports accent color
- **THEN** the `ThemeChangedEvent.currentTheme.accentColor` SHALL contain the current hex accent color string

#### Scenario: No duplicate events for redundant theme notifications

- **WHEN** the OS fires multiple theme change notifications that result in the same effective theme
- **THEN** the service SHALL deduplicate and fire only one `ThemeChangedEvent`

### Requirement: TypeScript types are generated for theme DTOs

The source generator SHALL produce TypeScript declarations for `ThemeInfo`, `ThemeChangedEvent`, and `IThemeService` methods.

#### Scenario: Generated bridge.d.ts includes ThemeInfo interface

- **WHEN** a project references `IThemeService` with `[JsExport]`
- **THEN** the generated `bridge.d.ts` SHALL include `ThemeInfo` with `mode: string`, `accentColor: string | null`, `isHighContrast: boolean`

#### Scenario: Generated bridge.d.ts includes ThemeChangedEvent

- **WHEN** a project references `IThemeService` with `[JsExport]`
- **THEN** the generated `bridge.d.ts` SHALL include `ThemeChangedEvent` with `currentTheme: ThemeInfo` and `previousMode: string`

### Requirement: Platform theme provider is abstracted and testable

The system SHALL define an `IPlatformThemeProvider` interface that abstracts OS-specific theme detection, enabling contract testing without platform dependencies.

#### Scenario: Mock platform provider enables contract testing

- **WHEN** contract tests substitute a mock `IPlatformThemeProvider`
- **THEN** `ThemeService` SHALL return theme data from the mock without requiring a real OS theme API

#### Scenario: Platform provider returns defaults on unsupported platforms

- **WHEN** `IPlatformThemeProvider` runs on a platform without accent color support
- **THEN** `GetAccentColor()` SHALL return `null` without throwing

### Requirement: Theme service is registered via standard bridge Expose pattern

The `IThemeService` SHALL be registered via `Bridge.Expose<IThemeService>(implementation)` following the existing bridge service pattern.

#### Scenario: Theme service is accessible from JS after registration

- **WHEN** host calls `Bridge.Expose<IThemeService>(themeService)`
- **THEN** JS can call `window.agWebView.bridge.ThemeService.getCurrentTheme()` and receive a valid response

#### Scenario: Theme event subscription works after bridge ready

- **WHEN** JS subscribes to the theme changed event after bridge is ready
- **THEN** the subscription SHALL receive subsequent theme change notifications
