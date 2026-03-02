## 1. Core Contracts

- [x] 1.1 Define `IThemeService` interface with `[JsExport]`: `GetCurrentTheme()`, `GetAccentColor()`, `GetHighContrastMode()`, and `IBridgeEvent<ThemeChangedEvent> ThemeChanged`
- [x] 1.2 Define DTOs: `ThemeInfo` (Mode, AccentColor, IsHighContrast), `ThemeChangedEvent` (CurrentTheme, PreviousMode)
- [x] 1.3 Define `IPlatformThemeProvider` interface: `GetThemeMode()`, `GetAccentColor()`, `GetIsHighContrast()`, `ThemeChanged` event

## 2. Platform Theme Providers

- [x] 2.1 Implement `AvaloniaThemeProvider` using `Application.ActualThemeVariant` and `ActualThemeVariantChanged` for dark/light detection
- [x] 2.2 Implement Windows accent color detection via `UISettings.GetColorValue` (conditional compilation or runtime check)
- [x] 2.3 Implement macOS accent color detection via platform interop (conditional)
- [x] 2.4 Implement fallback provider returning defaults for unsupported platforms (Linux accent, mobile)
- [x] 2.5 Add unit tests for each platform provider with mock OS APIs

## 3. Theme Service Implementation

- [x] 3.1 Implement `ThemeService : IThemeService` using `IPlatformThemeProvider`
- [x] 3.2 Wire `IPlatformThemeProvider.ThemeChanged` to `IBridgeEvent<ThemeChangedEvent>` with deduplication
- [x] 3.3 Add contract tests: GetCurrentTheme with mock provider, GetAccentColor null on unsupported, event firing on theme change, deduplication
- [x] 3.4 Add contract tests: verify TypeScript declaration generation includes ThemeInfo and ThemeChangedEvent

## 4. Integration and Samples

- [x] 4.1 Register `IThemeService` in template `app-shell` preset
- [x] 4.2 Add theme-aware demo to `samples/avalonia-react/` (display current theme, react to theme change events)
- [x] 4.3 Add integration test: expose theme service → call from JS → verify response
- [x] 4.4 Add integration test: trigger theme change → verify JS receives ThemeChangedEvent
