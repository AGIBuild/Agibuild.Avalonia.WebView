## ADDED Requirements

### Requirement: macOS WKWebView integration smoke tests exist (M0)
The system SHALL provide a macOS-only Integration Test (IT) smoke suite that exercises a real WKWebView-backed adapter end-to-end.

The smoke suite SHALL validate, at minimum:
- native-initiated navigation interception via `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
- redirect correlation behavior using a stable `CorrelationId` across a single redirect chain
- cancellation behavior where `Cancel=true` denies the native step and completes the correlated navigation as `Canceled`
- minimal script execution and WebMessage receive behavior (bridge enabled path)

#### Scenario: Smoke suite covers link click navigation
- **WHEN** a page in WKWebView triggers a main-frame navigation via a user link click
- **THEN** the IT suite observes `NavigationStarted` and `NavigationCompleted` for the same `NavigationId`

#### Scenario: Smoke suite covers 302 redirect correlation
- **WHEN** a main-frame navigation results in one or more HTTP 302 redirects within the same logical navigation chain
- **THEN** the adapter-host callback is invoked for each redirect step using the same `CorrelationId` and the final completion is reported exactly once for the host-issued `NavigationId`

#### Scenario: Smoke suite covers script-driven navigation
- **WHEN** a page triggers a main-frame navigation via `window.location`
- **THEN** the IT suite observes native-initiated navigation interception and a successful `NavigationCompleted`

#### Scenario: Smoke suite covers cancellation via NavigationStarted.Cancel
- **WHEN** an app handler sets `Cancel=true` for a native-initiated navigation step
- **THEN** the native step is denied and `NavigationCompleted` is raised with status `Canceled` for the corresponding `NavigationId`

#### Scenario: Smoke suite covers minimal script + WebMessage receive path
- **WHEN** the app invokes `InvokeScriptAsync` and the page posts a WebMessage on the configured channel
- **THEN** the IT suite observes a script result (when applicable) and a `WebMessageReceived` event for the instance channel
