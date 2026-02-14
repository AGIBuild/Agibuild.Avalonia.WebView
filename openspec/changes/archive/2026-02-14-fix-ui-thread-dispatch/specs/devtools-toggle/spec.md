## MODIFIED Requirements

### Requirement: DT-2: IWebView surface
Public DevTools APIs on `IWebView` SHALL be asynchronous:
- `Task OpenDevToolsAsync()`
- `Task CloseDevToolsAsync()`
- `Task<bool> IsDevToolsOpenAsync()`

When adapter does not implement `IDevToolsAdapter`, these operations SHALL be no-op semantics (or return `false` for state query) and MUST complete successfully.

#### Scenario: Open and close DevTools via async surface
- **WHEN** `OpenDevToolsAsync()` and then `CloseDevToolsAsync()` are called
- **THEN** both operations complete without threading contract exceptions

#### Scenario: Query DevTools state via async surface
- **WHEN** `IsDevToolsOpenAsync()` is called
- **THEN** it returns current state as async result
