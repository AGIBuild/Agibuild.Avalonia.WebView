## MODIFIED Requirements

### Requirement: ICommandManager defines standard editing commands
The `ICommandManager` interface SHALL define asynchronous methods:
- `Task CopyAsync()`
- `Task CutAsync()`
- `Task PasteAsync()`
- `Task SelectAllAsync()`
- `Task UndoAsync()`
- `Task RedoAsync()`

#### Scenario: ICommandManager methods are available
- **WHEN** a consumer reflects on `ICommandManager`
- **THEN** all six async methods are present

### Requirement: IWebView control exposes command manager
The `WebView` control SHALL expose `TryGetCommandManager()` returning a non-null `ICommandManager` when the adapter supports it.

Commands executed through `ICommandManager` SHALL complete as async operations and SHALL be routed through runtime operation queue semantics.

#### Scenario: Consumer can call CopyAsync on WebView
- **WHEN** `await webView.TryGetCommandManager()!.CopyAsync()` is called
- **THEN** the WebView selected content is copied and the returned Task completes
