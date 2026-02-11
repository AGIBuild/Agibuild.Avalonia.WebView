## Context

The project's event pattern (`NavigationStarted`, `DownloadRequested`, etc.) provides a natural model for context menu interception. The `Handled` flag pattern (used in `NewWindowRequested`, `WebResourceRequested`) maps directly: set `Handled = true` to suppress the native context menu.

## Goals / Non-Goals

**Goals:**
- Fire `ContextMenuRequested` event with hit-test info before native menu shows
- Allow suppressing native menu via `Handled = true`
- Provide enough info for consumers to build custom Avalonia menus
- Implement on all 5 platforms

**Non-Goals:**
- Providing a built-in replacement menu UI (consumer responsibility)
- Modifying the native context menu items (complex, platform-specific)
- Spell-check suggestions (separate feature)

## Decisions

1. **New adapter facet `IContextMenuAdapter`**
   ```csharp
   public interface IContextMenuAdapter
   {
       event EventHandler<ContextMenuRequestedEventArgs>? ContextMenuRequested;
   }
   ```

2. **ContextMenuRequestedEventArgs**
   ```csharp
   public sealed class ContextMenuRequestedEventArgs : EventArgs
   {
       public double X { get; init; }
       public double Y { get; init; }
       public Uri? LinkUri { get; init; }
       public string? SelectionText { get; init; }
       public ContextMenuMediaType MediaType { get; init; }
       public Uri? MediaSourceUri { get; init; }
       public bool IsEditable { get; init; }
       public bool Handled { get; set; }
   }

   public enum ContextMenuMediaType { None, Image, Video, Audio }
   ```

3. **Event flow:**
   ```
   Native context menu trigger
     → Adapter fires ContextMenuRequested
       → WebViewCore marshals to UI thread
         → Consumer handles event, optionally sets Handled = true
           → If Handled: native menu suppressed
           → If not Handled: native menu shows normally
   ```

4. **Platform mapping:**
   - Windows: `CoreWebView2.ContextMenuRequested` event (rich hit-test data available)
   - macOS: intercept via `willOpenMenu:withEvent:` on the WKWebView's menu delegate
   - iOS: `UIContextMenuInteraction` delegate or JS `contextmenu` event + native suppress
   - Android: `setOnCreateContextMenuListener()` + `WebView.HitTestResult`
   - GTK: `context-menu` signal on `WebKitWebView` (returns `WebKitContextMenu` + `WebKitHitTestResult`)

5. **Hit-test data availability:**
   - Windows/GTK: full native hit-test (link, image, selection, editable)
   - macOS: partial via JS (need `document.elementFromPoint` + link/img/contenteditable detection)
   - Android: `HitTestResult` provides type + extra (link URL, image URL) but limited
   - iOS: JS-based hit testing

## Risks / Trade-offs

- Hit-test data richness varies by platform. Windows and GTK provide comprehensive native info; macOS/iOS/Android may need JS augmentation. Accept that some fields may be null on some platforms.
- Suppressing native context menu reliably on macOS requires native-level interception (not just JS `preventDefault`). May need native shim changes.
- On iOS, long-press context menus have platform UX expectations that users may not want to break. Document the trade-off.
