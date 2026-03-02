## MODIFIED Requirements

### Requirement: WebView shortcut routing provides deterministic gesture-to-action execution with global shortcut coexistence

The system SHALL provide a reusable shortcut router that maps keyboard gestures to WebView actions with deterministic matching semantics. When a gesture matches both a window-local binding and a registered global shortcut, the window-local binding SHALL take priority.

#### Scenario: Default shell bindings include common editing commands and DevTools

- **WHEN** host creates shortcut router with default shell bindings
- **THEN** the binding set includes `Copy`, `Cut`, `Paste`, `SelectAll`, `Undo`, `Redo`, and `OpenDevTools` actions

#### Scenario: Matching shortcut executes mapped command action

- **WHEN** a keyboard gesture matches a configured command binding
- **THEN** runtime executes the mapped `ICommandManager` operation and returns a handled success result

#### Scenario: Window-local binding takes priority over global shortcut

- **WHEN** a keyboard gesture matches both a window-local shortcut binding and a registered global shortcut
- **AND** the application window IS focused
- **THEN** the window-local binding SHALL execute and the global shortcut trigger event SHALL be suppressed
