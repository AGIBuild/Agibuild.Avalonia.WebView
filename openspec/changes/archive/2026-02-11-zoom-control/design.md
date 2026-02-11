## Context

All 5 platform WebView engines support programmatic zoom. The project's facet pattern makes this straightforward: a new `IZoomAdapter` facet, detected by `WebViewCore`, exposed as a `ZoomFactor` property.

## Goals / Non-Goals

**Goals:**
- Expose `ZoomFactor` (get/set, double, 1.0 = 100%) on `WebView` / `WebViewCore` / `WebDialog`
- Fire `ZoomFactorChanged` event when zoom changes (programmatic or user-initiated)
- Support reasonable range (0.25–5.0, matching Chromium defaults)
- Implement on all 5 platforms

**Non-Goals:**
- Text-only zoom (full page zoom only)
- Pinch-to-zoom gesture configuration (platform default behavior)
- Per-origin zoom persistence (consumer responsibility)

## Decisions

1. **Facet interface `IZoomAdapter`**
   ```csharp
   public interface IZoomAdapter
   {
       double ZoomFactor { get; set; }
       event EventHandler<double>? ZoomFactorChanged;
   }
   ```

2. **Clamping** — `WebViewCore` clamps values to [0.25, 5.0] before delegating to adapter. Out-of-range values are silently clamped (no exception).

3. **Avalonia property** — `WebView.ZoomFactor` is a styled `StyledProperty<double>` with default 1.0, enabling XAML binding: `<webview:WebView ZoomFactor="1.5" />`.

4. **Platform mapping:**
   - Windows: `CoreWebView2Controller.ZoomFactor` (double, 1.0 = 100%)
   - macOS: `WKWebView.pageZoom` (CGFloat, 1.0 = 100%)
   - iOS: `WKWebView.scrollView.setZoomScale()` (partial — iOS WKWebView doesn't have direct page zoom)
   - Android: `WebSettings.setTextZoom(int percent)` → convert double ↔ int percent
   - GTK: `webkit_web_view_set_zoom_level(double)` (1.0 = 100%)

## Risks / Trade-offs

- iOS has no direct `pageZoom` equivalent on `WKWebView`. Alternatives: viewport meta tag manipulation or `scrollView.zoomScale`. Both have quirks. Accept iOS as ⚠️ with platform difference note.
- `ZoomFactorChanged` event from user pinch-to-zoom may not be observable on all platforms. Document as best-effort.
