## Context

Fulora's bridge architecture supports `[JsExport]` services with `IBridgeEvent<T>` for push-based event channels. The existing capability bridge handles policy governance for host operations. However, there is no bridge service for OS theme detection or native-to-web theme synchronization.

Avalonia provides `Application.Current.ActualThemeVariant` (Light/Dark/Default) and `Application.ActualThemeVariantChanged` event for theme monitoring. Platform-specific APIs can provide additional metadata like accent color (Windows: `UISettings.GetColorValue`, macOS: `NSColor.controlAccentColor`).

## Goals / Non-Goals

**Goals:**
- Provide `IThemeService` as a `[JsExport]` bridge service with `GetCurrentTheme()`, `GetAccentColor()`, `GetHighContrastMode()` methods
- Push OS theme changes to web content via `IBridgeEvent<ThemeChangedEvent>` in real-time
- Generate TypeScript types for all theme DTOs automatically via existing source generator
- Integrate with capability bridge policy model for optional theme access authorization
- Support all desktop platforms (Windows, macOS, Linux); mobile returns defaults

**Non-Goals:**
- Auto-applying CSS themes (provide data, not styling)
- Custom theme definitions beyond OS-provided values
- Per-window theme isolation
- Theme persistence or preferences storage

## Decisions

### D1: Service architecture — standalone [JsExport] vs capability bridge operation

**Choice**: Standalone `[JsExport]` service (`IThemeService`) rather than adding a new `WebViewHostCapabilityOperation`.

**Rationale**: Theme is a read-only observation service, not a mutating host capability. It doesn't need policy-governed allow/deny semantics for reads. The `IBridgeEvent<T>` pattern is the natural fit for push notifications, and `[JsExport]` gives us compile-time TypeScript types for free.

**Alternative considered**: Adding `ThemeRead` operation to `WebViewHostCapabilityOperation` — rejected because it overcomplicates the capability bridge for a simple read operation, and capability bridge events use a different dispatch model than `IBridgeEvent<T>`.

### D2: Theme detection — Avalonia API vs platform-specific APIs

**Choice**: Layered approach. Use Avalonia `ActualThemeVariant` for dark/light detection (cross-platform). Use platform-specific APIs behind `IPlatformThemeProvider` for accent color and high-contrast mode.

**Rationale**: Avalonia gives us dark/light for free on all platforms. Accent color and high-contrast are platform-specific:
- Windows: `UISettings` API
- macOS: `NSColor.controlAccentColor` via ObjC interop
- Linux: GTK theme settings (limited)

### D3: Event delivery — polling vs native event subscription

**Choice**: Native event subscription. Subscribe to `Application.ActualThemeVariantChanged` and platform-specific change notifications.

**Rationale**: Polling wastes resources and has latency. Avalonia and OS APIs all provide change events.

### D4: DTO design

**Choice**: `ThemeInfo` record with `Mode` (string: "light"/"dark"/"system"), `AccentColor` (string: hex "#RRGGBB" or null), `IsHighContrast` (bool). `ThemeChangedEvent` wraps `ThemeInfo` with `PreviousMode`.

**Rationale**: String-based mode for JS friendliness (maps to `prefers-color-scheme` values). Hex color for CSS compatibility. Nullable accent for platforms that don't support it.

## Testing Strategy

- **Contract tests**: Mock `IPlatformThemeProvider` → test `ThemeService` returns correct DTO values, fires events on theme change
- **Unit tests**: Platform theme provider isolation tests for each platform
- **Integration tests**: Register theme service via bridge → call from JS → verify theme data returned

## Risks / Trade-offs

- **[Platform coverage]** Linux accent color detection is limited (GTK doesn't expose it reliably) → Mitigation: return null for accent on unsupported platforms, document limitation
- **[Event timing]** Theme change event must fire after Avalonia finishes applying the new theme → Mitigation: subscribe to `ActualThemeVariantChanged` which fires post-application
- **[Mobile]** iOS/Android theme detection works differently (system dark mode) → Mitigation: Avalonia's `ActualThemeVariant` abstracts this; accent color returns null on mobile
