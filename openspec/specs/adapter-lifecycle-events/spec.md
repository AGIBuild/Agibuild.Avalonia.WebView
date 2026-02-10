## Requirements

### Requirement: AdapterCreated event is raised when the adapter is attached
The `IWebView` interface SHALL define an `AdapterCreated` event with `AdapterCreatedEventArgs`.
`WebViewCore` SHALL raise `AdapterCreated` immediately after `Attach()` succeeds and before any pending navigation is started.
The `AdapterCreatedEventArgs` SHALL carry an `IPlatformHandle? PlatformHandle` property containing the typed native handle.

#### Scenario: AdapterCreated fires after Attach with platform handle
- **WHEN** `WebViewCore.Attach(parentHandle)` completes successfully
- **THEN** the `AdapterCreated` event is raised with a non-null `PlatformHandle` (if the adapter implements `INativeWebViewHandleProvider`)

#### Scenario: AdapterCreated fires before pending navigation
- **WHEN** `Source` was set before `Attach()` and a pending navigation exists
- **THEN** the `AdapterCreated` event is raised before `NavigationStarted` is raised for the pending navigation

#### Scenario: AdapterCreated fires exactly once per attach
- **WHEN** `Attach()` is called
- **THEN** `AdapterCreated` is raised exactly once

### Requirement: AdapterDestroyed event is raised when the adapter is torn down
The `IWebView` interface SHALL define an `AdapterDestroyed` event with `EventHandler`.
`WebViewCore` SHALL raise `AdapterDestroyed` before calling `Detach()` on the adapter, giving subscribers a last chance to release their own native references.

#### Scenario: AdapterDestroyed fires before Detach
- **WHEN** the control is being removed from the visual tree and `Detach()` is about to be called
- **THEN** the `AdapterDestroyed` event is raised before `adapter.Detach()` executes

#### Scenario: AdapterDestroyed fires at most once
- **WHEN** both `Detach()` and `Dispose()` are called on the same `WebViewCore` instance
- **THEN** the `AdapterDestroyed` event is raised at most once

#### Scenario: No events after AdapterDestroyed
- **WHEN** `AdapterDestroyed` has been raised
- **THEN** no further `NavigationStarted`, `NavigationCompleted`, or other events are raised by this `WebViewCore` instance

### Requirement: TryGetWebViewHandle returns null after AdapterDestroyed
After `AdapterDestroyed` is raised, `TryGetWebViewHandle()` SHALL return `null`.

#### Scenario: Handle is null after destroyed
- **WHEN** `AdapterDestroyed` has been raised
- **THEN** `TryGetWebViewHandle()` returns `null`

### Requirement: WebView control bubbles lifecycle events
The `WebView` Avalonia control SHALL subscribe to `WebViewCore`'s lifecycle events and re-raise them as its own `AdapterCreated` and `AdapterDestroyed` events.

#### Scenario: WebView.AdapterCreated fires when core raises it
- **WHEN** the underlying `WebViewCore` raises `AdapterCreated`
- **THEN** the `WebView` control raises its own `AdapterCreated` event with the same args

#### Scenario: WebView.AdapterDestroyed fires when core raises it
- **WHEN** the underlying `WebViewCore` raises `AdapterDestroyed`
- **THEN** the `WebView` control raises its own `AdapterDestroyed` event

### Requirement: AdapterCreatedEventArgs definition
The Core assembly SHALL define `AdapterCreatedEventArgs` inheriting from `EventArgs` with:
- `IPlatformHandle? PlatformHandle { get; }` — the typed native WebView handle, or `null` if the adapter does not support handle exposure.

#### Scenario: AdapterCreatedEventArgs is resolvable
- **WHEN** a consumer references `AdapterCreatedEventArgs`
- **THEN** it compiles without missing type errors and exposes `PlatformHandle`
