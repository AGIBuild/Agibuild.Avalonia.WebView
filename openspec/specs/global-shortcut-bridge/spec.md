## Purpose

Define requirements for the global keyboard shortcut service exposed through the bridge.

## Requirements

### Requirement: IGlobalShortcutService exposes OS-level global hotkey registration via typed bridge

The system SHALL provide an `IGlobalShortcutService` interface decorated with `[JsExport]` that allows JS to register, unregister, and query OS-level global keyboard shortcuts.

#### Scenario: Register a global shortcut successfully

- **WHEN** JS calls `globalShortcutService.register({ id: "toggle-overlay", key: "Space", modifiers: ["Ctrl", "Shift"] })`
- **AND** the capability policy allows the registration
- **AND** no conflict exists with another application
- **THEN** the OS-level global hotkey SHALL be registered and the method SHALL return a success result

#### Scenario: Register a global shortcut denied by policy

- **WHEN** JS calls `globalShortcutService.register(binding)` but the capability policy denies the registration
- **THEN** the method SHALL return a denied result with reason metadata
- **AND** no OS-level hotkey SHALL be registered

#### Scenario: Register a conflicting global shortcut returns failure

- **WHEN** JS calls `globalShortcutService.register(binding)` with a key combination already held by another application
- **THEN** the method SHALL return a conflict failure result without throwing

#### Scenario: Unregister a previously registered shortcut

- **WHEN** JS calls `globalShortcutService.unregister("toggle-overlay")`
- **THEN** the OS-level global hotkey SHALL be removed and the method SHALL return success

#### Scenario: IsRegistered returns correct state

- **WHEN** JS calls `globalShortcutService.isRegistered("toggle-overlay")` after successful registration
- **THEN** the method SHALL return `true`

#### Scenario: GetRegistered returns all active registrations

- **WHEN** JS calls `globalShortcutService.getRegistered()`
- **THEN** the method SHALL return an array of all currently registered `GlobalShortcutBinding` objects

### Requirement: Global shortcut activation pushes event to JS via BridgeEvent

The `IGlobalShortcutService` SHALL expose an `IBridgeEvent<GlobalShortcutTriggeredEvent>` that fires when a registered global shortcut is triggered by the user.

#### Scenario: Pressing registered hotkey fires trigger event

- **WHEN** the user presses Ctrl+Shift+Space while the application is NOT focused
- **AND** a global shortcut with that combination is registered
- **THEN** the bridge SHALL push a `GlobalShortcutTriggeredEvent` to JS containing the shortcut `id` and a UTC timestamp

#### Scenario: Pressing registered hotkey while app is focused defers to window-local router

- **WHEN** the user presses a key combination that matches both a global shortcut and a `WebViewShortcutRouter` binding
- **AND** the application window IS focused
- **THEN** the window-local `WebViewShortcutRouter` binding SHALL take priority
- **AND** the global shortcut trigger event SHALL NOT fire

### Requirement: Platform provider is abstracted for testability

The system SHALL define an `IGlobalShortcutPlatformProvider` interface that abstracts OS-specific global hotkey APIs.

#### Scenario: Mock platform provider enables contract testing

- **WHEN** contract tests substitute a mock `IGlobalShortcutPlatformProvider`
- **THEN** `GlobalShortcutService` SHALL register/unregister shortcuts and fire events using the mock without requiring real OS hotkey APIs

#### Scenario: Unsupported platform returns IsSupported false

- **WHEN** `IGlobalShortcutPlatformProvider.IsSupported` returns `false` (e.g., Linux Wayland)
- **THEN** `register()` SHALL return a platform-unsupported result without throwing

### Requirement: All registered shortcuts are cleaned up on dispose

The `IGlobalShortcutService` SHALL unregister all OS-level global hotkeys when the service is disposed.

#### Scenario: Disposing service unregisters all shortcuts

- **WHEN** the service is disposed with 3 active global shortcut registrations
- **THEN** all 3 OS-level hotkeys SHALL be unregistered
- **AND** subsequent `isRegistered()` calls SHALL return `false`

### Requirement: Duplicate registration for same ID is rejected

The service SHALL reject registration attempts when a shortcut with the same ID is already registered.

#### Scenario: Registering duplicate ID returns error

- **WHEN** JS calls `register({ id: "x", ... })` twice without unregistering
- **THEN** the second call SHALL return a duplicate-id error result without modifying the existing registration
