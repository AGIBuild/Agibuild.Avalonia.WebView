# Screenshot Capture — Design

## Architecture

```
Consumer
  │
  ▼
IWebView.CaptureScreenshotAsync() → Task<byte[]> (PNG)
  │
  ▼
WebViewCore (detects IScreenshotAdapter facet)
  │
  ▼
IScreenshotAdapter.CaptureScreenshotAsync()
  │
  ├── Windows: CoreWebView2.CapturePreviewAsync(Stream) → PNG
  ├── macOS:   WKWebView.takeSnapshot(with:completionHandler:) → NSImage → PNG
  ├── iOS:     WKWebView.takeSnapshot(with:completionHandler:) → UIImage → PNG
  ├── GTK:     webkit_web_view_get_snapshot() → cairo_surface → PNG
  └── Android: AWebView.draw(Canvas) → Bitmap → PNG byte[]
```

## Core Contracts

```csharp
// Add to IWebView:
Task<byte[]> CaptureScreenshotAsync();
```

## Adapter Facet

```csharp
public interface IScreenshotAdapter
{
    Task<byte[]> CaptureScreenshotAsync();
}
```

## Design Decisions

1. **PNG format only** — Keep it simple. PNG is lossless and universally supported.
2. **Full viewport capture** — Captures the visible viewport, not the full scrollable content.
3. **Optional facet** — `CaptureScreenshotAsync()` throws `NotSupportedException` if adapter doesn't implement `IScreenshotAdapter`.
4. **Native shim extensions needed** — macOS/iOS need native shim functions for `takeSnapshot` callback marshaling. GTK needs `get_snapshot` async callback. Windows and Android can be done purely in C#.
