## MODIFIED Requirements

### Requirement: AddPreloadScript registers early JS injection
`WebViewCore.AddPreloadScriptAsync(string javaScript)` SHALL register a JS script to be injected at document start (before page scripts) on every navigation. It SHALL return a script ID for later removal.

#### Scenario: Script runs before page JS
- **GIVEN** a preload script `window.__myBridge = { ready: true }`
- **WHEN** a page loads and runs `console.log(window.__myBridge.ready)`
- **THEN** the value is `true`

#### Scenario: Script persists across navigations
- **GIVEN** a registered preload script
- **WHEN** the user navigates to a new page
- **THEN** the preload script runs on the new page as well

### Requirement: RemovePreloadScript unregisters a script
`WebViewCore.RemovePreloadScriptAsync(string scriptId)` SHALL remove a previously registered preload script.

#### Scenario: Removed script no longer runs
- **GIVEN** a registered preload script with ID "abc"
- **WHEN** `await RemovePreloadScriptAsync("abc")` is called and a new page loads
- **THEN** the script does not run on the new page

### Requirement: IPreloadScriptAdapter facet detection
`WebViewCore` SHALL detect `IPreloadScriptAdapter` on the adapter at construction time. If absent, `AddPreloadScriptAsync` SHALL fail with `NotSupportedException`.

#### Scenario: Adapter supports preload
- **WHEN** adapter implements `IPreloadScriptAdapter`
- **THEN** `AddPreloadScriptAsync` delegates to adapter through runtime operation queue

#### Scenario: Adapter lacks preload support
- **WHEN** adapter does not implement `IPreloadScriptAdapter`
- **THEN** `AddPreloadScriptAsync` fails with `NotSupportedException`

### Requirement: WebView and WebDialog expose preload API
Both `WebView` and `WebDialog` SHALL expose `AddPreloadScriptAsync` / `RemovePreloadScriptAsync`.

#### Scenario: WebView delegates to core
- **WHEN** `await webView.AddPreloadScriptAsync(js)` is called
- **THEN** it delegates to the underlying `WebViewCore`
