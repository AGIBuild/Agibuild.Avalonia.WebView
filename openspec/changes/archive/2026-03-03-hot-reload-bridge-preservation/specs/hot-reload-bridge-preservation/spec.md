# Hot Reload Bridge Preservation — Spec

## Purpose

Define BDD-style requirements for bridge state preservation across HMR (Hot Module Replacement) reloads: HMR detection, state serialization to sessionStorage, restoration of pending calls and event subscriptions, and host-side preservation. Ensures developers retain bridge state during SPA hot reload for a smoother dev loop.

## Requirements

### Requirement: Bridge client detects HMR dispose

The bridge client SHALL detect HMR dispose events and serialize state before the module is replaced.

#### Scenario: Vite HMR dispose triggers serialization
- **GIVEN** the app runs under Vite with HMR enabled
- **AND** `import.meta.hot` is available
- **WHEN** HMR triggers a module replacement (e.g., file save)
- **THEN** the bridge client SHALL register a dispose callback via `import.meta.hot.dispose`
- **AND** the callback SHALL run before the module is replaced
- **AND** the callback SHALL serialize pending calls and subscription metadata to sessionStorage

#### Scenario: webpack HMR dispose triggers serialization
- **GIVEN** the app runs under webpack with HMR enabled
- **AND** `module.hot` is available
- **WHEN** HMR triggers a module replacement
- **THEN** the bridge client SHALL register a dispose callback via `module.hot.dispose`
- **AND** the callback SHALL serialize state to sessionStorage before replacement

#### Scenario: No HMR API skips serialization
- **GIVEN** neither `import.meta.hot` nor `module.hot` is available
- **WHEN** the bridge client is created
- **THEN** no HMR dispose callback SHALL be registered
- **AND** no serialization SHALL occur on "dispose"

### Requirement: Pending calls are serialized and restored

The bridge client SHALL serialize in-flight (pending) RPC calls to sessionStorage and re-invoke them on reconnect.

#### Scenario: Pending calls are stored on dispose
- **GIVEN** the bridge client has one or more in-flight RPC calls (e.g., `AppService.GetData()` not yet resolved)
- **WHEN** HMR dispose runs
- **THEN** each pending call SHALL be serialized with at least: method name, params, correlation ID
- **AND** the serialized data SHALL be written to sessionStorage under a documented key

#### Scenario: Pending calls are re-invoked on restore
- **GIVEN** sessionStorage contains serialized pending calls from a previous HMR cycle
- **WHEN** the bridge client initializes after HMR apply
- **THEN** the client SHALL read the serialized state from sessionStorage
- **AND** SHALL re-invoke each pending call via the RPC
- **AND** SHALL clear the sessionStorage key after successful restore

#### Scenario: Restore provides results to app
- **GIVEN** pending calls were restored and re-invoked
- **WHEN** the re-invoked calls complete
- **THEN** the bridge client SHALL provide a way for the app to receive results (e.g., callback, event, or promise list)
- **AND** the app MAY use these results to restore UI state

### Requirement: Event subscription metadata is preserved

The bridge client SHALL preserve event subscription metadata (service, event name) and signal the app to re-subscribe on restore.

#### Scenario: Subscription metadata is stored on dispose
- **GIVEN** the app has subscribed to bridge events (e.g., `AppService.onDataChanged`)
- **WHEN** HMR dispose runs
- **THEN** the bridge client SHALL serialize subscription metadata (service name, event name) to sessionStorage
- **AND** callback references SHALL NOT be stored (not serializable)

#### Scenario: Restore signals app to re-subscribe
- **GIVEN** sessionStorage contains subscription metadata
- **WHEN** the bridge client restores after HMR
- **THEN** the client SHALL emit an event or invoke a callback (e.g., `onBridgeRestored` or `bridgeRestored`) with the subscription list
- **AND** the app SHALL use this signal to re-register event subscriptions
- **AND** the bridge client SHALL clear the sessionStorage key after restore

### Requirement: PreserveStateOnReload option controls behavior

The bridge client SHALL respect a `PreserveStateOnReload` option to enable or disable HMR state preservation.

#### Scenario: Preservation enabled in dev mode by default
- **GIVEN** the app is running in development mode (`import.meta.env.DEV` or `NODE_ENV === 'development'`)
- **WHEN** `createBridgeClient` is called without explicit `PreserveStateOnReload`
- **THEN** `PreserveStateOnReload` SHALL default to true
- **AND** HMR dispose and restore logic SHALL be active

#### Scenario: Preservation disabled in production
- **GIVEN** the app is running in production mode
- **WHEN** the bridge client is created
- **THEN** `PreserveStateOnReload` SHALL default to false
- **AND** HMR state preservation SHALL NOT be active (no dispose/restore registration)

#### Scenario: Explicit option overrides default
- **GIVEN** the caller passes `PreserveStateOnReload: false` to `createBridgeClient`
- **WHEN** the bridge client is created
- **THEN** HMR state preservation SHALL NOT be active regardless of environment
- **AND** when `PreserveStateOnReload: true` is passed in production, preservation SHALL be active (for testing)

### Requirement: Host-side services remain exposed across HMR

The host-side bridge service SHALL NOT require re-exposure of services after HMR; existing registrations SHALL remain valid.

#### Scenario: Host services persist across HMR
- **GIVEN** the host has exposed `IAppService` via `Expose<IAppService>(impl)`
- **WHEN** the WebView's JavaScript is replaced by HMR (module re-execution)
- **THEN** the host's `RuntimeBridgeService` SHALL still have `IAppService` registered
- **AND** re-invoked calls from the restored bridge client SHALL reach the host successfully
- **AND** no host-side changes SHALL be required for reconnection

### Requirement: sessionStorage key and format

The serialized state SHALL use a documented sessionStorage key and JSON format.

#### Scenario: State uses documented key
- **WHEN** state is written to sessionStorage
- **THEN** the key SHALL be a documented constant (e.g., `agibuild.bridge.hmr.state`)
- **AND** the value SHALL be a JSON string

#### Scenario: State format includes pendingCalls and subscriptions
- **GIVEN** serialized state
- **WHEN** the JSON is parsed
- **THEN** it SHALL have structure `{ pendingCalls: [...], subscriptions: [...] }` (or equivalent)
- **AND** `pendingCalls` SHALL include method, params, and correlation id for each call
- **AND** `subscriptions` SHALL include service and event for each subscription
