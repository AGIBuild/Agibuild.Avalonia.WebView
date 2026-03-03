# Official Plugin: Notifications — Design

## Context

Hybrid apps need to show system notifications (toast/banner). The JavaScript Notification API is restricted or unavailable in WebView contexts. A bridge plugin routes notification display through the host OS notification APIs, supporting Windows, macOS, Linux, Android, and iOS consistently.

**Existing contracts**: `IBridgePlugin`, `UsePlugin<T>`, NuGet+npm dual distribution. Reference plugin: LocalStorage. Bridge supports `[JsImport]` for host-to-JS callbacks.

## Goals / Non-Goals

### Goals

- Cross-platform system notifications via bridge from JavaScript
- Permission model mirroring OS behavior (request before show)
- Notification click callback routed from host to JS
- Unique notification ID tracking for click routing and clearAll
- Platform adapters for all 5 target platforms

### Non-Goals

- Push notifications (remote)
- Notification scheduling
- Notification grouping/channels in v1

## Decisions

### D1: Platform adapter pattern (INativeNotificationProvider)

**Decision**: The plugin SHALL use an `INativeNotificationProvider` abstraction per platform. Each platform adapter implements this interface. The `NotificationService` delegates all native operations to the resolved provider. Providers are registered via DI or platform detection at runtime.

**Rationale**: Platform APIs differ significantly (Windows ToastNotification, macOS/iOS UNUserNotificationCenter, Linux libnotify, Android NotificationManager). A provider abstraction isolates platform-specific code and enables testability via mock providers. Single implementation per platform keeps the plugin maintainable.

### D2: Notification ID tracking

**Decision**: Each notification SHALL have a unique string ID. The plugin generates IDs (e.g., GUID) when `show` is called. IDs are passed to the native provider and stored in a registry. When a notification is clicked, the host receives the ID from the OS and routes it to the callback. `clearAll` uses the ID registry to clear all notifications shown by the plugin.

**Rationale**: OS notification APIs return or accept identifiers. Tracking IDs enables click callback routing (which notification was clicked) and selective clearing. String IDs are bridge-friendly and avoid serialization issues.

### D3: Click callback routing via [JsImport] INotificationCallback

**Decision**: The plugin SHALL define `[JsImport] INotificationCallback` with `onNotificationClicked(string id)`. The host registers a proxy implementation that invokes the JS callback when the OS reports a notification click. The bridge runtime handles the host-to-JS invocation.

**Rationale**: Notification clicks occur on the host; the app must react (e.g., focus window, navigate). `[JsImport]` is the bridge pattern for host-initiated calls to JS. The callback receives the notification ID so JS can correlate with the original `show` call.

### D4: Permission model mirroring OS behavior

**Decision**: The plugin SHALL expose `requestPermission()` returning a permission status (granted, denied, default). Before `show`, the plugin SHALL check permission; if not granted, `show` MAY fail or the plugin MAY call `requestPermission` internally (configurable). Permission state SHALL be cached per session and re-requested when the OS indicates a change.

**Rationale**: macOS, iOS, and Android require explicit permission. Windows and Linux are typically permissive. Mirroring OS behavior ensures consistent UX and avoids runtime failures. Caching reduces redundant permission dialogs.

### D5: show(title, body, options) options

**Decision**: The `show` method SHALL accept optional `NotificationOptions`: `icon` (URL or base64), `tag` (grouping hint for future use), `silent` (no sound), `requireInteraction` (stay until dismissed). Platform adapters SHALL map these to native equivalents where supported; unsupported options are ignored.

**Rationale**: Common notification customization needs. Not all platforms support all options; graceful degradation keeps the API simple. `tag` reserved for future grouping support.

### D6: clearAll scope

**Decision**: `clearAll()` SHALL clear only notifications shown by this plugin in the current app session. It SHALL NOT clear notifications from other apps or system notifications. Platform adapters SHALL use the ID registry to target the correct notifications.

**Rationale**: Clearing all system notifications would be invasive. Scoping to plugin-originated notifications is predictable and safe.

## Risks / Trade-offs

### R1: Platform API availability

**Risk**: libnotify may not be installed on all Linux distros; some platforms have different notification stacks.

**Mitigation**: Document platform requirements. Provide fallback (e.g., in-app toast) or fail gracefully with clear error. Consider optional platform packages.

### R2: Click callback timing

**Risk**: User clicks notification when app is backgrounded; callback may fire before bridge is ready or after app has been torn down.

**Mitigation**: Bridge runtime handles reconnection. Store pending click IDs if needed. Document lifecycle expectations in plugin docs.

### R3: Permission persistence across restarts

**Risk**: OS may reset permission state; cached permission may be stale.

**Mitigation**: Re-check permission at plugin init or before first show. Allow host to force re-request via options.
