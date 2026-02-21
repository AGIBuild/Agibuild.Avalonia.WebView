## Purpose
Define command-manager contracts for editing commands across runtime and adapters.

## Requirements

### Requirement: ICommandManager defines standard editing commands
The `ICommandManager` interface SHALL define synchronous methods:
- `void Copy()`
- `void Cut()`
- `void Paste()`
- `void SelectAll()`
- `void Undo()`
- `void Redo()`

#### Scenario: ICommandManager methods are available
- **WHEN** a consumer reflects on `ICommandManager`
- **THEN** all six methods are present

### Requirement: WebViewCommand enum in Core
The Core assembly SHALL define a `WebViewCommand` enum with members: `Copy`, `Cut`, `Paste`, `SelectAll`, `Undo`, `Redo`.

#### Scenario: WebViewCommand enum is resolvable
- **WHEN** a consumer references `WebViewCommand`
- **THEN** it compiles with all six members

### Requirement: ICommandAdapter facet for adapters
The adapter abstractions SHALL define an `ICommandAdapter` interface:
- `void ExecuteCommand(WebViewCommand command)`

The runtime SHALL detect `ICommandAdapter` via type check at initialization.

#### Scenario: Adapter implementing ICommandAdapter enables command manager
- **WHEN** an adapter implements both `IWebViewAdapter` and `ICommandAdapter`
- **THEN** `TryGetCommandManager()` returns a non-null `ICommandManager`

#### Scenario: Adapter without ICommandAdapter returns null
- **WHEN** an adapter implements only `IWebViewAdapter`
- **THEN** `TryGetCommandManager()` returns `null`

### Requirement: All platform adapters implement ICommandAdapter
All five platform adapters (Windows, macOS, iOS, GTK, Android) SHALL implement `ICommandAdapter` using platform-appropriate APIs.

#### Scenario: Each adapter executes editing commands
- **WHEN** `ExecuteCommand(WebViewCommand.Copy)` is called
- **THEN** the adapter invokes the platform's copy command on the WebView

### Requirement: WebView control exposes command manager
The `WebView` Avalonia control SHALL expose `TryGetCommandManager()` returning a non-null `ICommandManager` when the adapter supports it.

#### Scenario: Consumer can call Copy on WebView
- **WHEN** `webView.TryGetCommandManager()?.Copy()` is called
- **THEN** the WebView's selected content is copied to the clipboard
