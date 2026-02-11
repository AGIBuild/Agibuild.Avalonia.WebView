## Context

The project uses a facet-based adapter pattern: optional capabilities are exposed via marker interfaces (`ICommandAdapter`, `IScreenshotAdapter`, etc.) that platform adapters implement. `WebViewCore` detects facets at construction and exposes them through the public API. This change follows the same pattern for find-in-page.

## Goals / Non-Goals

**Goals:**
- Expose `FindInPageAsync(text, options?)` and `StopFindInPage()` on `WebView` / `WebViewCore` / `WebDialog`
- Provide match count and active match index via `FindInPageResult` event
- Support case-sensitive toggle and forward/backward navigation
- Implement on all 5 platforms using native find APIs

**Non-Goals:**
- Custom highlight colors (platform-dependent, not portable)
- Regex-based search (no native support on most platforms)
- Find-in-page UI (consumers build their own; we provide the API)

## Decisions

1. **Facet interface `IFindInPageAdapter`** — follows existing pattern (ICommandAdapter, etc.)
   ```csharp
   public interface IFindInPageAdapter
   {
       Task<FindInPageResult> FindAsync(string text, FindInPageOptions? options);
       void StopFind(bool clearHighlights = true);
       event EventHandler<FindInPageResult>? FindResultChanged;
   }
   ```

2. **FindInPageOptions** — minimal, portable options
   ```csharp
   public sealed class FindInPageOptions
   {
       public bool CaseSensitive { get; init; }
       public bool Forward { get; init; } = true;
   }
   ```

3. **FindInPageResult** — event args for match info
   ```csharp
   public sealed class FindInPageResult : EventArgs
   {
       public int ActiveMatchIndex { get; init; }
       public int TotalMatches { get; init; }
   }
   ```

4. **Platform mapping:**
   - Windows: `CoreWebView2` → custom JS `window.find()` (FindController not available in all WebView2 versions)
   - macOS: `WKWebView` → `evaluateJavaScript("window.find()")` or `NSTextFinder`
   - iOS: same as macOS (`evaluateJavaScript`)
   - Android: `WebView.findAllAsync()` / `findNext()` / `clearMatches()`
   - GTK: `webkit_find_controller_search()` / `search_next()` / `search_finish()`

## Risks / Trade-offs

- `window.find()` JS API has inconsistent behavior across engines (especially match counting). Android's native `findAllAsync` is more reliable. GTK's `WebKitFindController` is the gold standard. We may need platform-specific implementations rather than a universal JS approach.
- Match count is not available via `window.find()` alone — may need DOM traversal or platform-native APIs. Accept that match counting may be best-effort on some platforms.
