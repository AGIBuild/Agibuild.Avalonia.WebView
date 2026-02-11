# Tasks — find-in-page

## 1. Core Contracts
- [x] Add `FindInPageOptions` class to `WebViewContracts.cs`
- [x] Add `FindInPageResult` class to `WebViewContracts.cs`
- [x] Add `IFindInPageAdapter` facet interface to `IWebViewAdapter.cs`

## 2. Runtime Integration
- [x] Add `IFindInPageAdapter` facet detection in `WebViewCore` constructor
- [x] Add `FindInPageAsync(string, FindInPageOptions?)` to `WebViewCore`
- [x] Add `StopFindInPage()` to `WebViewCore`
- [x] Expose `FindInPageAsync` / `StopFindInPage` on `WebDialog`
- [x] Expose `FindInPageAsync` / `StopFindInPage` on `WebView`

## 3. Platform Adapters — macOS
- [x] Add native `ag_wk_find_text` / `ag_wk_stop_find` to `WkWebViewShim.mm`
- [x] Add P/Invoke declarations in `MacOSWebViewAdapter.PInvoke.cs`
- [x] Implement `IFindInPageAdapter` in macOS adapter

## 4. Platform Adapters — Windows
- [x] Implement `IFindInPageAdapter` in `WindowsWebViewAdapter.cs`

## 5. Platform Adapters — iOS
- [x] Add native find functions to `WkWebViewShim.iOS.mm`
- [x] Implement `IFindInPageAdapter` in `iOSWebViewAdapter.cs`

## 6. Platform Adapters — Android
- [x] Implement `IFindInPageAdapter` in `AndroidWebViewAdapter.cs` using `findAllAsync`/`findNext`/`clearMatches`

## 7. Platform Adapters — GTK
- [x] Add native `ag_gtk_find_text` / `ag_gtk_stop_find` to `WebKitGtkShim.c`
- [x] Implement `IFindInPageAdapter` in `GtkWebViewAdapter.cs`

## 8. Testing
- [x] Add `MockWebViewAdapterWithFind` to `MockWebViewAdapter.cs`
- [x] Add contract tests: facet detection, find delegation, NotSupportedException
- [x] Add integration tests (headless): find + stop via mock adapter
- [x] Verify all tests pass
- [x] Verify coverage >= 90%
