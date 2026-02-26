## Why

Right-click context menus in embedded browsers need customization. Bundled-browser stacks provide `context-menu` event with hit-test info (link URL, selected text, image URL, editable state). Our WebView has no context menu interception — the platform default menu always shows. Consumers need to either suppress the default menu, augment it, or replace it entirely with an Avalonia-native menu for consistent UX.

## What Changes

- Add `IContextMenuAdapter` facet interface for platform adapters
- Add `ContextMenuRequested` event on `WebViewCore` / `WebDialog` / `WebView`
- Add `ContextMenuRequestedEventArgs` with:
  - `Position` (x, y coordinates)
  - `LinkUri` (if right-clicked on a link)
  - `SelectionText` (if text is selected)
  - `MediaType` (None, Image, Video, Audio)
  - `MediaSourceUri` (if right-clicked on media)
  - `IsEditable` (if right-clicked in an editable field)
  - `Handled` (set to true to suppress native context menu)
- Implement native context menu interception on all 5 platform adapters:
  - Windows: `CoreWebView2.ContextMenuRequested`
  - macOS: override `willOpenMenu:withEvent:` or swizzle
  - iOS: `UIContextMenuInteraction` or long-press gesture
  - Android: `WebView.setOnCreateContextMenuListener()`
  - GTK: `context-menu` signal on `WebKitWebView`

## Capabilities

### New Capabilities
- `context-menu`: Context menu interception with hit-test information

### Modified Capabilities
- `webview-compatibility-matrix`: Add context-menu as Extended capability entry

## Impact

- New facet interface in `IWebViewAdapter.cs`
- New event + event args on `WebView`, `WebViewCore`, `WebDialog`
- Native shim additions for macOS/iOS, GTK
- Platform adapter implementations (5 adapters)
- New unit + integration tests
