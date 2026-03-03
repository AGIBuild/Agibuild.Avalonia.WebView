# Official Plugin: Notifications — Tasks

## 1. Project Setup

- [x] 1.1 Create `src/Agibuild.Fulora.Plugin.Notifications/` project with .csproj targeting net8.0
- [x] 1.2 Add package references: `Agibuild.Fulora` (or Bridge core)
- [x] 1.3 Add `fulora-plugin` and `fulora-plugin-notifications` to `PackageTags`
- [x] 1.4 Create `fulora-plugin.json` manifest with id, displayName, services, npmPackage
- [x] 1.5 Configure manifest as content file packed at package root
- [x] 1.6 Add platform-specific project references or conditional compilation for Windows, macOS, Linux, Android, iOS

## 2. INotificationService Contract

- [x] 2.1 Define `INotificationService` interface with [JsExport] and methods: `ShowAsync(title, body, options?)`, `RequestPermissionAsync()`, `ClearAllAsync()`
- [x] 2.2 Define `NotificationOptions` DTO: icon, tag, silent, requireInteraction
- [x] 2.3 Define `NotificationPermission` enum or string: granted, denied, default
- [x] 2.4 Define `[JsImport] INotificationCallback` with `OnNotificationClicked(string id)`
- [x] 2.5 Define `NotificationsPluginOptions` for callback registration, permission behavior

## 3. Platform Adapters (INativeNotificationProvider)

- [x] 3.1 Define `INativeNotificationProvider` interface: ShowAsync, RequestPermissionAsync, ClearAsync, ClearAllAsync
- [x] 3.2 Implement Windows adapter using ToastNotification (or Microsoft.Toolkit.Uwp.Notifications)
- [x] 3.3 Implement macOS adapter using UNUserNotificationCenter
- [x] 3.4 Implement iOS adapter using UNUserNotificationCenter
- [x] 3.5 Implement Linux adapter using libnotify or DBus notifications
- [x] 3.6 Implement Android adapter using NotificationManager
- [x] 3.7 Implement provider resolution (platform detection, DI registration)

## 4. Notification ID Tracking and Click Callback

- [x] 4.1 Implement notification ID generation (GUID or similar) in NotificationService
- [x] 4.2 Maintain ID registry mapping IDs to platform notification handles
- [x] 4.3 Wire native click events to INotificationCallback.OnNotificationClicked
- [x] 4.4 Register JsImport callback proxy with bridge when plugin initializes
- [x] 4.5 Handle callback when no JS handler registered (no-op, no crash)

## 5. Permission Model

- [x] 5.1 Implement permission state caching per session
- [x] 5.2 Implement RequestPermissionAsync delegating to platform provider
- [x] 5.3 Implement permission check before ShowAsync; fail or auto-request per options
- [x] 5.4 Map platform permission results to granted/denied/default

## 6. NotificationService Implementation

- [x] 6.1 Implement `NotificationService : INotificationService` delegating to INativeNotificationProvider
- [x] 6.2 Implement `NotificationsPlugin : IBridgePlugin` with GetServices()
- [x] 6.3 Wire plugin to accept NotificationsPluginOptions (callback, permission behavior)
- [x] 6.4 Implement clearAll using ID registry to target plugin-originated notifications only

## 7. npm Package

- [x] 7.1 Create `packages/bridge-plugin-notifications/` with package.json
- [x] 7.2 Generate or hand-write TypeScript types for INotificationService methods and NotificationOptions
- [x] 7.3 Export `getNotificationService()` helper that resolves service from bridge client
- [x] 7.4 Export INotificationCallback interface and registration helper for JS
- [x] 7.5 Publish npm package as `@agibuild/bridge-plugin-notifications`

## 8. Tests

- [x] 8.1 Unit tests: NotificationService with mock INativeNotificationProvider — verify ShowAsync, RequestPermissionAsync, ClearAllAsync
- [x] 8.2 Unit tests: ID generation and registry — verify unique IDs, clearAll targets correct notifications
- [x] 8.3 Unit tests: Permission flow — verify show fails when denied, succeeds when granted
- [x] 8.4 Unit tests: Click callback routing — verify provider invokes callback with correct ID
- [x] 8.5 Integration test: Full flow from JS show through bridge to mock provider, click callback round-trip
- [x] 8.6 Platform-specific tests (or documented manual verification) for each adapter
