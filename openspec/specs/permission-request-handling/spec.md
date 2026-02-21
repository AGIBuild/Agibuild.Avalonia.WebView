## Purpose
Define permission request contracts for policy control and adapter event propagation.

## Requirements

### Requirement: WebViewPermissionKind enum in Core
The Core assembly SHALL define `WebViewPermissionKind` enum with members:
`Unknown`, `Camera`, `Microphone`, `Geolocation`, `Notifications`, `ClipboardRead`, `ClipboardWrite`, `Midi`, `Sensors`, `Other`

#### Scenario: Permission kind enum is resolvable
- **WHEN** a consumer references `WebViewPermissionKind`
- **THEN** it compiles without missing type errors

### Requirement: PermissionState enum in Core
The Core assembly SHALL define `PermissionState` enum with members:
`Default` (let platform handle), `Allow`, `Deny`

#### Scenario: PermissionState enum is resolvable
- **WHEN** a consumer references `PermissionState`
- **THEN** it compiles without missing type errors

### Requirement: PermissionRequestedEventArgs type in Core
The Core assembly SHALL define `PermissionRequestedEventArgs : EventArgs` with:
- `WebViewPermissionKind PermissionKind { get; }` — the type of permission requested
- `Uri? Origin { get; }` — the origin requesting the permission
- `PermissionState State { get; set; }` — consumer sets Allow/Deny/Default

#### Scenario: Event args carry permission metadata
- **WHEN** a web page requests camera access
- **THEN** `PermissionRequested` is raised with `PermissionKind == Camera` and `Origin` matching the page origin

### Requirement: IWebView includes PermissionRequested event
The `IWebView` interface SHALL define:
- `event EventHandler<PermissionRequestedEventArgs>? PermissionRequested`

#### Scenario: PermissionRequested event is available on IWebView
- **WHEN** a consumer subscribes to `IWebView.PermissionRequested`
- **THEN** the subscription compiles without error

### Requirement: IPermissionAdapter facet for adapters
The adapter abstractions SHALL define `IPermissionAdapter`:
- `event EventHandler<PermissionRequestedEventArgs>? PermissionRequested`

Adapters MAY implement `IPermissionAdapter` alongside `IWebViewAdapter`.
The runtime SHALL detect `IPermissionAdapter` support via type check and subscribe to its events.

#### Scenario: Adapter implementing IPermissionAdapter enables permission events
- **WHEN** an adapter implements `IPermissionAdapter`
- **THEN** `WebViewCore` subscribes to `PermissionRequested` and forwards to consumers

#### Scenario: Adapter without IPermissionAdapter silently skips
- **WHEN** an adapter does NOT implement `IPermissionAdapter`
- **THEN** `PermissionRequested` is never raised and no error occurs

### Requirement: Consumer can control permission via event args
The runtime SHALL honor consumer permission decisions when `PermissionRequested` is raised:
- If consumer sets `State = Allow`, the permission SHALL be granted
- If consumer sets `State = Deny`, the permission SHALL be denied
- If `State` remains `Default`, the platform's native handling applies

#### Scenario: Consumer allows camera permission
- **WHEN** the handler sets `State = PermissionState.Allow`
- **THEN** the native permission is granted

#### Scenario: Consumer denies geolocation permission
- **WHEN** the handler sets `State = PermissionState.Deny`
- **THEN** the native permission is denied

#### Scenario: Default state uses platform behavior
- **WHEN** the handler does not modify `State`
- **THEN** the platform shows its native permission dialog or applies default policy

### Requirement: All platform adapters implement IPermissionAdapter
All five platform adapters SHALL implement `IPermissionAdapter` using the appropriate native API.

#### Scenario: Windows adapter handles permissions via PermissionRequested
- **WHEN** WebView2 triggers `PermissionRequested`
- **THEN** the adapter raises `PermissionRequested` and applies consumer State

#### Scenario: macOS/iOS adapter handles permissions via WKUIDelegate
- **WHEN** WKWebView requests media capture permission
- **THEN** the adapter raises `PermissionRequested`

#### Scenario: Android adapter handles permissions via onPermissionRequest
- **WHEN** Android WebView triggers `onPermissionRequest`
- **THEN** the adapter raises `PermissionRequested`

### Requirement: WebView control bubbles PermissionRequested
The `WebView` Avalonia control SHALL subscribe to `WebViewCore`'s `PermissionRequested` and re-raise it.

#### Scenario: WebView.PermissionRequested fires
- **WHEN** the underlying WebViewCore raises PermissionRequested
- **THEN** the WebView control raises its own PermissionRequested with the same args
