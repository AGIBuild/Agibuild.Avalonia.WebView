## 1. Icon Resolution

- [x] 1.1 Define `ITrayIconResolver` interface with `Resolve(string iconPath)` returning `WindowIcon?`
- [x] 1.2 Implement `AvaloniaResourceIconResolver` for `avares://` URIs
- [x] 1.3 Implement `FilePathIconResolver` for local file paths
- [x] 1.4 Implement `CompositeIconResolver` that chains resolvers with fallback
- [x] 1.5 Add unit tests for each resolver (valid resource, valid file, invalid path, null input)

## 2. Tray Manager

- [x] 2.1 Implement `AvaloniaTrayManager` that wraps Avalonia `TrayIcon` lifecycle (create, update, hide, dispose)
- [x] 2.2 Wire `ITrayIconResolver` into tray manager for icon source resolution
- [x] 2.3 Subscribe to `TrayIcon.Clicked` event and expose an `Action<TrayInteractionEventArgs>` callback
- [x] 2.4 Ensure all `TrayIcon` mutations dispatch to `Dispatcher.UIThread`
- [x] 2.5 Add unit tests: show/hide transitions, tooltip updates, icon updates, dispose cleanup

## 3. Menu Manager

- [x] 3.1 Implement `AvaloniaMenuManager` that maps `WebViewMenuItemModel` tree to `NativeMenu`/`NativeMenuItem` hierarchy recursively
- [x] 3.2 Handle `IsEnabled` mapping and separator items
- [x] 3.3 Subscribe to `NativeMenuItem.Click` for leaf items, expose `Action<MenuInteractionEventArgs>` callback with item ID
- [x] 3.4 Implement menu clearing (empty model → clear all items)
- [x] 3.5 Ensure all `NativeMenu` mutations dispatch to `Dispatcher.UIThread`
- [x] 3.6 Add unit tests: flat menu, nested menu, disabled items, empty model, click dispatch

## 4. Avalonia Host Capability Provider

- [x] 4.1 Implement `AvaloniaHostCapabilityProvider : IWebViewHostCapabilityProvider` delegating tray ops to `AvaloniaTrayManager` and menu ops to `AvaloniaMenuManager`
- [x] 4.2 Implement passthrough for existing operations (clipboard, file dialog, external open, notification, system action) to preserve current behavior
- [x] 4.3 Wire inbound event callbacks from tray/menu managers back to the shell experience dispatch path
- [x] 4.4 Implement `IDisposable` — dispose tray manager, menu manager, and unsubscribe events
- [x] 4.5 Add platform guard: return no-op for tray/menu on non-desktop platforms (iOS, Android)
- [x] 4.6 Add contract tests: full capability matrix (allow/deny for tray and menu operations)

## 5. Integration and Samples

- [x] 5.1 Wire `AvaloniaHostCapabilityProvider` into template `app-shell` preset as the default provider
- [x] 5.2 Add tray + menu demonstration to `samples/avalonia-react/` (show tray icon, apply menu model from JS)
- [x] 5.3 Add integration test: expose bridge service → call tray update from JS → verify tray state
- [x] 5.4 Add integration test: apply menu model from JS → click menu item → verify JS receives interaction event
