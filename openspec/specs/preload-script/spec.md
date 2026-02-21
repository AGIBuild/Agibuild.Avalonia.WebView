## Purpose
Define preload script contracts for early injection, removal, and adapter capability checks.

## Requirements

### Requirement: AddPreloadScript registers early JS injection
`WebViewCore.AddPreloadScript(string javaScript)` SHALL register a JS script to be injected at document start (before page scripts) on every navigation. Returns a string script ID for later removal.

#### Scenario: Script runs before page JS
- **GIVEN** a preload script `window.__myBridge = { ready: true }`
- **WHEN** a page loads and runs `console.log(window.__myBridge.ready)`
- **THEN** the value is `true` (preload ran first)

#### Scenario: Script persists across navigations
- **GIVEN** a registered preload script
- **WHEN** the user navigates to a new page
- **THEN** the preload script runs on the new page as well

### Requirement: RemovePreloadScript unregisters a script
`WebViewCore.RemovePreloadScript(string scriptId)` SHALL remove a previously registered preload script.

#### Scenario: Removed script no longer runs
- **GIVEN** a registered preload script with ID "abc"
- **WHEN** `RemovePreloadScript("abc")` is called and a new page loads
- **THEN** the script does not run on the new page

### Requirement: IPreloadScriptAdapter facet detection
`WebViewCore` SHALL detect `IPreloadScriptAdapter` on the adapter. If absent, `AddPreloadScript` SHALL throw `NotSupportedException`.

#### Scenario: Adapter supports preload
- **WHEN** adapter implements `IPreloadScriptAdapter`
- **THEN** `AddPreloadScript` delegates to the adapter

#### Scenario: Adapter lacks preload support
- **WHEN** adapter does not implement `IPreloadScriptAdapter`
- **THEN** `AddPreloadScript` throws `NotSupportedException`

### Requirement: Global preload via WebViewEnvironmentOptions
`WebViewEnvironmentOptions.PreloadScripts` SHALL contain a list of JS strings applied to all new WebView instances at construction.

#### Scenario: Global preload applied
- **GIVEN** `WebViewEnvironment.Options.PreloadScripts` contains a script
- **WHEN** a new WebView is created
- **THEN** the script is registered as a preload script

### Requirement: WebView and WebDialog expose preload API
Both `WebView` and `WebDialog` SHALL expose `AddPreloadScript` / `RemovePreloadScript`.

#### Scenario: WebView delegates to core
- **WHEN** `webView.AddPreloadScript(js)` is called
- **THEN** it delegates to the underlying `WebViewCore`
