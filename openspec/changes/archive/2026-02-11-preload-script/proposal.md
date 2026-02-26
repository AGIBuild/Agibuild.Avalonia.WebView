## Why

Bundled-browser stacks' `webPreferences.preload` lets apps inject JS before page scripts run, enabling secure bridge setup, polyfills, and API injection. Our `InvokeScriptAsync` only runs after page load, and the RPC JS stub is auto-injected but not user-configurable. Consumers migrating from bundled-browser stacks need a way to inject custom JS early to set up their own bridge APIs, intercept globals, or establish communication channels before page code executes.

## What Changes

- Add `PreloadScripts` collection property to `WebViewEnvironmentOptions` (applied globally) and per-`WebView` instance
- Each preload script is a string of JS code injected before page scripts run
- Implement via platform-specific user script injection:
  - Windows: `CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync()`
  - macOS/iOS: `WKUserContentController.addUserScript()` with injection time `.atDocumentStart`
  - Android: `WebViewClient.onPageStarted()` + `evaluateJavascript()`
  - GTK: `webkit_user_content_manager_add_script()` with `WEBKIT_USER_SCRIPT_INJECT_AT_DOCUMENT_START`
- Add `AddPreloadScript(string js)` / `RemovePreloadScript(string js)` methods

## Capabilities

### New Capabilities
- `preload-script`: Early JS injection before page scripts execute

### Modified Capabilities
- `webview-compatibility-matrix`: Add preload-script as Extended capability entry

## Impact

- New public API on `WebView`, `WebViewCore` (AddPreloadScript / RemovePreloadScript)
- New property on `WebViewEnvironmentOptions` (PreloadScripts)
- Native shim additions for macOS/iOS (WKUserContentController), GTK (webkit_user_content_manager)
- Platform adapter implementations (5 adapters)
- New unit + integration tests
