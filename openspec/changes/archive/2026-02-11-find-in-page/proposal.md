## Why

Embedded browser scenarios (e.g., documentation viewers, email clients, code preview panels) commonly need in-page text search. Electron exposes `webContents.findInPage()` / `stopFindInPage()` for this. Our WebView currently has no equivalent, forcing consumers to build custom JS overlays that lack native selection highlighting and match counting.

## What Changes

- Add `IFindInPageAdapter` facet interface for platform adapters
- Add `FindInPageAsync(string text, FindInPageOptions?)` and `StopFindInPage()` to `WebViewCore` / `WebDialog` / `WebView`
- Add `FindInPageOptions` (case-sensitive, forward/backward, wrap-around)
- Add `FindInPageResult` event args (active match index, total match count)
- Implement native find-in-page on all 5 platform adapters:
  - Windows: `CoreWebView2.FindController` or `ExecuteScriptAsync` with `window.find()`
  - macOS: `WKWebView` → `performTextFinderAction` or `evaluateJavaScript` with `window.find()`
  - iOS: same as macOS
  - Android: `WebView.findAllAsync()` / `findNext()` / `clearMatches()`
  - GTK: `webkit_find_controller_*` APIs

## Capabilities

### New Capabilities
- `find-in-page`: In-page text search with match count, navigation between matches, and highlighting

### Modified Capabilities
- `webview-compatibility-matrix`: Add find-in-page as Extended capability entry

## Impact

- New facet interface in `IWebViewAdapter.cs`
- New public API on `WebView`, `WebViewCore`, `WebDialog`
- Native shim additions for macOS/iOS (WkWebViewShim), GTK (WebKitGtkShim)
- Platform adapter implementations (5 adapters)
- New unit + integration tests
