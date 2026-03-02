## Why

Hybrid applications need consistent visual theming between native and web layers. Currently there is no bridge service for OS theme detection or native↔web theme synchronization. Web content must rely on CSS `prefers-color-scheme` which only detects the browser-level setting and cannot react to host-initiated theme changes or expose rich theme metadata (accent color, high contrast mode). This creates a visual disconnect between native Avalonia UI and embedded web content.

**Goal IDs**: G1 (Type-Safe Bridge — new typed service), E3 (Hot Reload Integration — theme changes reflect instantly), G3 (Secure by Default — policy-governed theme access)

**ROADMAP justification**: Post-1.0 differentiation. Electron has no native theme bridge — apps must poll or use OS-specific Node APIs. Fulora can offer compile-time type-safe, event-driven theme synchronization as a unique advantage.

## What Changes

- Add `IThemeService` as a `[JsExport]` bridge service exposing: `GetCurrentTheme()`, `GetAccentColor()`, `GetHighContrastMode()`
- Add `IBridgeEvent<ThemeChangedEvent>` for push-based OS theme change notifications to JS
- Implement Avalonia-side theme detection using `Application.Current.RequestedThemeVariant` and platform APIs
- Generate TypeScript types for theme DTOs (`ThemeInfo`, `ThemeChangedEvent`, `AccentColor`)
- Integrate with `WebViewHostCapabilityBridge` policy model for theme access authorization

## Non-goals

- Applying themes to web content automatically (CSS injection) — the service provides data, web app decides styling
- Custom theme definitions beyond OS-provided values
- Per-window theme variance (single app-wide theme source)

## Capabilities

### New Capabilities
- `theme-sync-bridge`: Typed bridge service for OS theme detection, accent color retrieval, high-contrast mode awareness, and real-time theme change event push from host to web content

### Modified Capabilities
- None

## Impact

- `src/Agibuild.Fulora.Core/` — `IThemeService` interface, theme DTOs, `[JsExport]` attribute
- `src/Agibuild.Fulora.Runtime/` — Avalonia theme detection implementation, OS theme monitor
- `src/Agibuild.Fulora.Bridge.Generator/` — no changes (existing generator handles new service)
- `packages/bridge/` — no changes (existing `getService()` works with generated types)
- `tests/` — contract tests for theme service, mock theme provider
- `samples/avalonia-react/` — theme-aware demo (dark/light mode toggle synced with OS)
