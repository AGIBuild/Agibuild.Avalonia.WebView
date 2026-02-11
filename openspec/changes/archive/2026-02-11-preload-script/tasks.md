# Tasks — preload-script

## 1. Core Contracts
- [x] Add `IPreloadScriptAdapter` facet interface to `IWebViewAdapter.cs`

## 2. Runtime Integration
- [x] Add `IPreloadScriptAdapter` facet detection in `WebViewCore` constructor
- [x] Add `AddPreloadScript(string js) → string` to `WebViewCore`
- [x] Add `RemovePreloadScript(string scriptId)` to `WebViewCore`
- [x] Add `PreloadScripts` collection to `WebViewEnvironmentOptions`
- [x] Apply global preload scripts at `WebViewCore` construction
- [x] Expose `AddPreloadScript` / `RemovePreloadScript` on `WebDialog`
- [x] Expose `AddPreloadScript` / `RemovePreloadScript` on `WebView`

## 3. Platform Adapters — macOS
- [x] Add native `ag_wk_add_user_script` / `ag_wk_remove_user_script` to `WkWebViewShim.mm`
- [x] Add P/Invoke declarations in `MacOSWebViewAdapter.PInvoke.cs`
- [x] Implement `IPreloadScriptAdapter` in macOS adapter

## 4. Platform Adapters — Windows
- [x] Implement `IPreloadScriptAdapter` in `WindowsWebViewAdapter.cs` via `AddScriptToExecuteOnDocumentCreatedAsync`

## 5. Platform Adapters — iOS
- [x] Add native user script functions to `WkWebViewShim.iOS.mm`
- [x] Implement `IPreloadScriptAdapter` in `iOSWebViewAdapter.cs`

## 6. Platform Adapters — Android
- [x] Implement `IPreloadScriptAdapter` in `AndroidWebViewAdapter.cs` (inject in `onPageStarted`)

## 7. Platform Adapters — GTK
- [x] Add native `ag_gtk_add_user_script` / `ag_gtk_remove_user_script` to `WebKitGtkShim.c`
- [x] Implement `IPreloadScriptAdapter` in `GtkWebViewAdapter.cs`

## 8. Testing
- [x] Add `MockWebViewAdapterWithPreload` to `MockWebViewAdapter.cs`
- [x] Add contract tests: add/remove, NotSupportedException, global preloads
- [x] Add integration tests (headless): add/remove via mock adapter
- [x] Verify all tests pass
- [x] Verify coverage >= 90%
