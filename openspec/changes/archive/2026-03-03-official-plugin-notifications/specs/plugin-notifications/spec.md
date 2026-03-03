# Plugin Notifications — Spec

## Purpose

Define requirements for the Fulora Notifications bridge plugin. Enables cross-platform system notifications from JavaScript through the host OS APIs, with permission handling, click callbacks, and consistent behavior across Windows, macOS, Linux, Android, and iOS.

## Requirements

### Requirement: Plugin implements IBridgePlugin and exposes INotificationService

The Notifications plugin SHALL implement `IBridgePlugin` and expose `INotificationService` as a bridge service, following the established plugin convention.

#### Scenario: Plugin declares INotificationService via GetServices

- **WHEN** the Notifications plugin is registered via `Bridge.UsePlugin<NotificationsPlugin>()`
- **THEN** the plugin SHALL return a service descriptor for `INotificationService`
- **AND** the service SHALL be accessible from JS via the bridge under the registered service name

#### Scenario: Plugin has companion npm package

- **WHEN** the plugin is published
- **THEN** `@agibuild/bridge-plugin-notifications` SHALL be available on npm
- **AND** the npm package SHALL export TypeScript types for `INotificationService` methods and DTOs

---

### Requirement: INotificationService.show displays a system notification

The `INotificationService` SHALL expose `show(title, body, options)` to display a system notification.

#### Scenario: show with title and body displays notification

- **WHEN** JS calls `notifications.show("Hello", "World")` with valid title and body
- **THEN** the host SHALL display a system notification with the given title and body
- **AND** the method SHALL return a unique notification ID (string)
- **AND** the operation SHALL be async (returns Promise)

#### Scenario: show with options applies supported options

- **WHEN** JS calls `notifications.show(title, body, { icon: "...", silent: true })` with options
- **THEN** the host SHALL apply supported options (icon, silent, etc.) to the notification
- **AND** unsupported options on a platform SHALL be ignored without error
- **AND** the notification SHALL still be displayed

#### Scenario: show fails when permission denied

- **WHEN** permission has been denied (e.g., user declined)
- **AND** JS calls `show(title, body)`
- **THEN** the call SHALL fail with a clear error indicating permission denied
- **AND** no notification SHALL be displayed

---

### Requirement: requestPermission requests notification permission

The plugin SHALL expose `requestPermission()` to request notification permission from the user.

#### Scenario: requestPermission returns permission status

- **WHEN** JS calls `notifications.requestPermission()`
- **THEN** the host SHALL request permission from the OS (if not already determined)
- **AND** the method SHALL return a permission status: "granted", "denied", or "default"
- **AND** the operation SHALL be async

#### Scenario: requestPermission on already-granted permission returns granted

- **WHEN** permission was previously granted
- **AND** JS calls `requestPermission()`
- **THEN** the method SHALL return "granted" without showing a permission dialog
- **AND** no redundant OS permission prompt SHALL be shown

---

### Requirement: Notification click callback routes to JS

When a notification is clicked by the user, the host SHALL invoke the registered JS callback with the notification ID.

#### Scenario: onNotificationClicked invoked when user clicks notification

- **WHEN** a notification was shown via `show` and the user clicks it
- **THEN** the host SHALL invoke `INotificationCallback.onNotificationClicked(id)` with the notification ID
- **AND** the JS callback SHALL receive the same ID returned from `show`
- **AND** the invocation SHALL occur on the bridge's JS context

#### Scenario: Callback not invoked when notification dismissed without click

- **WHEN** the user dismisses a notification without clicking (e.g., swipe away, timeout)
- **THEN** `onNotificationClicked` SHALL NOT be invoked
- **AND** no error SHALL occur

#### Scenario: Callback registration

- **WHEN** the host needs to invoke the click callback
- **THEN** the plugin SHALL use the registered `[JsImport] INotificationCallback` proxy
- **AND** if no callback is registered, the click SHALL be ignored (no crash)

---

### Requirement: clearAll removes plugin-originated notifications

The plugin SHALL expose `clearAll()` to remove all notifications shown by the plugin in the current session.

#### Scenario: clearAll removes all plugin notifications

- **WHEN** JS calls `notifications.clearAll()`
- **THEN** the host SHALL remove all notifications that were shown via this plugin in the current app session
- **AND** the operation SHALL be async
- **AND** notifications from other apps SHALL NOT be affected

#### Scenario: clearAll when no notifications shown succeeds

- **WHEN** no notifications have been shown in the session
- **AND** JS calls `clearAll()`
- **THEN** the call SHALL succeed without error
- **AND** no exception SHALL be thrown

---

### Requirement: Platform-specific behavior

The plugin SHALL support all 5 target platforms with appropriate native adapters.

#### Scenario: Windows uses ToastNotification

- **WHEN** the app runs on Windows
- **THEN** the plugin SHALL use the Windows ToastNotification API (or equivalent)
- **AND** notifications SHALL appear in the Windows notification center

#### Scenario: macOS and iOS use UNUserNotificationCenter

- **WHEN** the app runs on macOS or iOS
- **THEN** the plugin SHALL use UNUserNotificationCenter
- **AND** permission SHALL be requested via the native permission flow

#### Scenario: Linux uses libnotify

- **WHEN** the app runs on Linux
- **THEN** the plugin SHALL use libnotify (or equivalent) for notifications
- **AND** the plugin SHALL document libnotify as a dependency or optional requirement

#### Scenario: Android uses NotificationManager

- **WHEN** the app runs on Android
- **THEN** the plugin SHALL use Android NotificationManager
- **AND** notification channels SHALL be created as required by Android API level

---

### Requirement: fulora-plugin.json manifest

The plugin SHALL include a `fulora-plugin.json` manifest for discovery and installation.

#### Scenario: Manifest includes required fields

- **WHEN** the Notifications plugin package is built
- **THEN** the package SHALL contain `fulora-plugin.json` at the package root
- **AND** the manifest SHALL include: `id`, `displayName`, `services` (including `INotificationService`), `npmPackage` (`@agibuild/bridge-plugin-notifications`)
