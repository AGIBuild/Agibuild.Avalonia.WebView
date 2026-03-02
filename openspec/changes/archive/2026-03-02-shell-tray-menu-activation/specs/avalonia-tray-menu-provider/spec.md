## ADDED Requirements

### Requirement: Avalonia provider binds TrayUpdateState to Avalonia TrayIcon

The `AvaloniaHostCapabilityProvider` SHALL implement `UpdateTrayState(WebViewTrayStateRequest)` by creating, updating, or hiding an Avalonia `TrayIcon` instance on the UI thread.

#### Scenario: Tray icon becomes visible when IsVisible is true

- **WHEN** `UpdateTrayState` is called with `IsVisible = true`, `Tooltip = "My App"`, and a valid `IconPath`
- **THEN** an Avalonia `TrayIcon` SHALL be created or updated with the specified tooltip and icon
- **AND** the tray icon SHALL be visible in the system tray

#### Scenario: Tray icon is hidden when IsVisible is false

- **WHEN** `UpdateTrayState` is called with `IsVisible = false`
- **THEN** the Avalonia `TrayIcon` SHALL be hidden or removed from the system tray

#### Scenario: Tray icon tooltip and icon update without recreation

- **WHEN** `UpdateTrayState` is called multiple times with different tooltip and icon values but `IsVisible = true`
- **THEN** the existing `TrayIcon` instance SHALL be updated in place without disposing and recreating

#### Scenario: UpdateTrayState on non-desktop platform returns unsupported result

- **WHEN** `UpdateTrayState` is called on a platform where Avalonia `TrayIcon` is not supported (iOS, Android)
- **THEN** the provider SHALL return without error and the capability bridge SHALL report a platform-unsupported outcome

### Requirement: Avalonia provider binds MenuApplyModel to Avalonia NativeMenu

The `AvaloniaHostCapabilityProvider` SHALL implement `ApplyMenuModel(WebViewMenuModelRequest)` by mapping the `WebViewMenuItemModel` tree to an Avalonia `NativeMenu` / `NativeMenuItem` hierarchy.

#### Scenario: Flat menu model maps to NativeMenu items

- **WHEN** `ApplyMenuModel` is called with a request containing 3 top-level `WebViewMenuItemModel` items
- **THEN** the Avalonia `NativeMenu` SHALL contain 3 `NativeMenuItem` instances with matching labels and enabled states

#### Scenario: Nested menu model maps to submenus recursively

- **WHEN** `ApplyMenuModel` is called with a `WebViewMenuItemModel` that has `Children` containing 2 items
- **THEN** the corresponding `NativeMenuItem` SHALL have a submenu `NativeMenu` with 2 child items

#### Scenario: Disabled menu item maps to NativeMenuItem with IsEnabled false

- **WHEN** a `WebViewMenuItemModel` has `IsEnabled = false`
- **THEN** the corresponding `NativeMenuItem` SHALL have `IsEnabled = false`

#### Scenario: Empty menu model clears the NativeMenu

- **WHEN** `ApplyMenuModel` is called with an empty items list
- **THEN** the Avalonia `NativeMenu` SHALL be cleared of all items

### Requirement: Tray click dispatches inbound TrayInteractionEvent

The `AvaloniaHostCapabilityProvider` SHALL dispatch a `TrayInteractionEventDispatch` inbound event through the capability bridge when the user clicks the tray icon.

#### Scenario: Single click on tray icon dispatches interaction event

- **WHEN** the user clicks the Avalonia `TrayIcon`
- **THEN** the provider SHALL invoke the inbound event callback with a `TrayInteractionEventDispatch` payload
- **AND** the event SHALL flow through the capability bridge to web content

#### Scenario: Tray interaction event includes timestamp

- **WHEN** a tray interaction event is dispatched
- **THEN** the event payload SHALL include a UTC timestamp normalized to millisecond precision

### Requirement: Menu item click dispatches inbound MenuInteractionEvent

The `AvaloniaHostCapabilityProvider` SHALL dispatch a `MenuInteractionEventDispatch` inbound event through the capability bridge when the user selects a menu item.

#### Scenario: Clicking a menu item dispatches interaction event with item ID

- **WHEN** the user clicks a `NativeMenuItem` mapped from a `WebViewMenuItemModel` with `Id = "settings"`
- **THEN** the provider SHALL invoke the inbound event callback with a `MenuInteractionEventDispatch` payload containing `ItemId = "settings"`

#### Scenario: Clicking a parent menu item with children does not dispatch event

- **WHEN** the user hovers or clicks a `NativeMenuItem` that has a submenu
- **THEN** no `MenuInteractionEventDispatch` event SHALL be dispatched (only leaf items trigger events)

### Requirement: Icon resolution supports multiple source types

The provider SHALL resolve tray icon sources from embedded resources (`avares://`), file paths, and stream-based sources via an `ITrayIconResolver` abstraction.

#### Scenario: Embedded resource icon resolves to WindowIcon

- **WHEN** `IconPath` is an `avares://` URI pointing to a valid embedded PNG/ICO resource
- **THEN** the resolver SHALL produce a valid `WindowIcon` for the `TrayIcon`

#### Scenario: File path icon resolves to WindowIcon

- **WHEN** `IconPath` is a local file path pointing to a valid image file
- **THEN** the resolver SHALL produce a valid `WindowIcon` for the `TrayIcon`

#### Scenario: Invalid icon path produces fallback or error

- **WHEN** `IconPath` is null, empty, or points to a nonexistent resource
- **THEN** the resolver SHALL return a default icon or the provider SHALL report a failure diagnostic

### Requirement: All Avalonia UI operations are dispatched to the UI thread

The provider SHALL marshal all `TrayIcon` and `NativeMenu` mutations to the Avalonia UI thread via `Dispatcher.UIThread`.

#### Scenario: UpdateTrayState called from background thread completes on UI thread

- **WHEN** `UpdateTrayState` is called from a non-UI thread
- **THEN** the tray icon mutation SHALL execute on the Avalonia UI thread without deadlock

#### Scenario: ApplyMenuModel called from background thread completes on UI thread

- **WHEN** `ApplyMenuModel` is called from a non-UI thread
- **THEN** the menu mutation SHALL execute on the Avalonia UI thread without deadlock

### Requirement: Provider is disposable and cleans up tray/menu resources

The `AvaloniaHostCapabilityProvider` SHALL implement `IDisposable` and clean up `TrayIcon` and `NativeMenu` resources on disposal.

#### Scenario: Disposing provider removes tray icon

- **WHEN** the provider is disposed
- **THEN** the `TrayIcon` SHALL be removed from the system tray and disposed

#### Scenario: Disposing provider clears NativeMenu

- **WHEN** the provider is disposed
- **THEN** the `NativeMenu` items SHALL be cleared and event subscriptions removed
