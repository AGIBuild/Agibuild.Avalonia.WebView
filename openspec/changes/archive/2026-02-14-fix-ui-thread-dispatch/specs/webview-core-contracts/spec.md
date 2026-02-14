## MODIFIED Requirements

### Requirement: IWebView contract surface
The `IWebView` interface SHALL expose an async-first command and control surface:
- navigation: `Task NavigateAsync(Uri uri)`, `Task NavigateToStringAsync(string html)`, `Task NavigateToStringAsync(string html, Uri? baseUrl)`, `Task<string?> InvokeScriptAsync(string script)`
- commands: `Task<bool> GoBackAsync()`, `Task<bool> GoForwardAsync()`, `Task<bool> RefreshAsync()`, `Task<bool> StopAsync()`
- command/property-like operations: `Task OpenDevToolsAsync()`, `Task CloseDevToolsAsync()`, `Task<bool> IsDevToolsOpenAsync()`
- zoom operations: `Task<double> GetZoomFactorAsync()`, `Task SetZoomFactorAsync(double zoomFactor)`
- preload operations: `Task<string> AddPreloadScriptAsync(string javaScript)`, `Task RemovePreloadScriptAsync(string scriptId)`
- find operations: `Task<FindInPageResult> FindInPageAsync(string text, FindInPageOptions? options = null)`, `Task StopFindInPageAsync(bool clearHighlights = true)`

Legacy synchronous command/property APIs for the same capabilities SHALL NOT be part of the public `IWebView` contract.

#### Scenario: IWebView members are available with async command surface
- **WHEN** a consumer reflects on `IWebView`
- **THEN** command/property-like operations are represented by async methods and legacy sync counterparts are absent

### Requirement: Core defines IWebViewDispatcher contract
The Core assembly SHALL define an `IWebViewDispatcher` interface to support:
- UI-thread identity checks
- deterministic marshaling of work onto the UI thread for async APIs and event delivery

The dispatcher contract SHALL remain async-only and SHALL NOT require synchronous blocking invoke methods.

#### Scenario: IWebViewDispatcher is resolvable
- **WHEN** a consumer references `IWebViewDispatcher` from the Core assembly
- **THEN** it compiles without missing type errors and provides async marshaling APIs

## ADDED Requirements

### Requirement: WebViewCore operation queue is the single adapter execution path
`WebViewCore` SHALL execute all adapter-backed operations through a single operation queue with one consumer. Each queued operation SHALL be dispatched to the UI thread before invoking adapter code.

No public API path (including manager/facade objects returned by `TryGet*` methods) SHALL bypass this queue.

#### Scenario: Manager path cannot bypass queue
- **WHEN** a consumer obtains `ICommandManager` via `TryGetCommandManager()` and executes a command
- **THEN** the underlying adapter call is executed through the same WebViewCore operation queue

### Requirement: WebViewCore defines deterministic lifecycle gate behavior
`WebViewCore` SHALL define explicit lifecycle states (`Created`, `Attaching`, `Ready`, `Detaching`, `Disposed`) and SHALL enforce deterministic operation acceptance/rejection by state.

Calls rejected due to lifecycle state SHALL fail fast with a deterministic error classification.

#### Scenario: Operations fail fast after disposal
- **WHEN** any adapter-backed API is invoked in `Disposed` state
- **THEN** the returned Task fails fast with disposal-classified error and is not queued
