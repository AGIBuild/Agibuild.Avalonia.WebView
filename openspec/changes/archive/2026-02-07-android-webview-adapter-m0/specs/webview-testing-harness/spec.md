## ADDED Requirements

### Requirement: Android WebView integration smoke tests exist (M0)
The system SHALL provide an Android-only Integration Test (IT) smoke suite that exercises a real Android WebView-backed adapter end-to-end.

The smoke suite SHALL validate, at minimum:
- native-initiated navigation interception via `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
- redirect correlation behavior using a stable `CorrelationId` across a single redirect chain
- cancellation behavior where `Cancel=true` denies the native step and completes the correlated navigation as `Canceled`
- minimal script execution and WebMessage receive behavior (bridge enabled path)
- cookie CRUD via `ICookieManager` backed by `android.webkit.CookieManager`
- navigation error categorization mapping from `WebViewClient.ERROR_*` codes
- `TryGetWebViewHandle()` returning a valid handle with descriptor `"AndroidWebView"`
- `NavigateToStringAsync(html, baseUrl)` with proper baseUrl resolution via `loadDataWithBaseURL`

#### Scenario: Smoke suite covers link click navigation
- **WHEN** a page in Android WebView triggers a main-frame navigation via a user link click
- **THEN** the IT suite observes `NavigationStarted` and `NavigationCompleted` for the same `NavigationId`

#### Scenario: Smoke suite covers redirect correlation
- **WHEN** a main-frame navigation results in one or more HTTP 302 redirects within the same logical navigation chain
- **THEN** the adapter-host callback is invoked with a stable `CorrelationId` and the final completion is reported exactly once for the host-issued `NavigationId`

#### Scenario: Smoke suite covers script-driven navigation
- **WHEN** a page triggers a main-frame navigation via `window.location`
- **THEN** the IT suite observes native-initiated navigation interception and a successful `NavigationCompleted`

#### Scenario: Smoke suite covers cancellation via Cancel
- **WHEN** an app handler sets `Cancel=true` for a native-initiated navigation step
- **THEN** the native step is denied and `NavigationCompleted` is raised with status `Canceled` for the corresponding `NavigationId`

#### Scenario: Smoke suite covers script + WebMessage receive path
- **WHEN** the app invokes `InvokeScriptAsync` and the page posts a WebMessage on the configured channel
- **THEN** the IT suite observes a script result and a `WebMessageReceived` event for the instance channel

#### Scenario: Smoke suite covers cookie CRUD
- **WHEN** a cookie is set via `ICookieManager.SetCookieAsync`, navigated to a page, and then queried via `GetCookiesAsync`
- **THEN** the cookie is present in the result

#### Scenario: Smoke suite covers network error categorization
- **WHEN** a navigation targets an unreachable host on Android
- **THEN** `NavigationCompleted.Error` is a `WebViewNetworkException`

#### Scenario: Smoke suite covers native handle
- **WHEN** the Android WebView adapter is attached
- **THEN** `TryGetWebViewHandle()` returns a handle with `HandleDescriptor == "AndroidWebView"`

#### Scenario: Smoke suite covers NavigateToStringAsync with baseUrl
- **WHEN** `NavigateToStringAsync(html, baseUrl)` is called with a non-null `baseUrl`
- **THEN** the loaded page resolves relative resources against `baseUrl` and the heading content is accessible via `InvokeScriptAsync`
