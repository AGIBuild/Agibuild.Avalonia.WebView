## Why

Hybrid apps need to show system notifications (toast/banner). JavaScript Notification API is restricted in WebView contexts. A bridge plugin routes notification display through the host OS notification APIs, supporting all 5 platforms consistently. Goal: Phase 11 M11.3.

## What Changes

- New NuGet: Agibuild.Fulora.Plugin.Notifications implementing IBridgePlugin
- New npm: @agibuild/bridge-plugin-notifications with TypeScript types
- [JsExport] INotificationService: show(title, body, options), requestPermission(), clearAll()
- Platform adapters for Windows (ToastNotification), macOS (UNUserNotificationCenter), Linux (libnotify), Android (NotificationManager), iOS (UNUserNotificationCenter)
- Notification click callback via [JsImport] INotificationCallback.onNotificationClicked(id)

## Capabilities

### New Capabilities
- `plugin-notifications`: Bridge plugin for cross-platform system notifications

### Modified Capabilities
(none)

## Non-goals

- Push notifications (remote), notification scheduling, notification grouping/channels in v1

## Impact

- New project: src/Agibuild.Fulora.Plugin.Notifications/
- New npm: packages/bridge-plugin-notifications/
- Platform-specific code per adapter
