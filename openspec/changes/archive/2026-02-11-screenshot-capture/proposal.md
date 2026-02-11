# Screenshot Capture

## Problem
Consumers cannot capture the current rendered content of the WebView as an image. This is needed for thumbnails, reports, and visual testing.

## Solution
Add `Task<byte[]> CaptureScreenshotAsync()` to `IWebView` that returns a PNG-encoded byte array of the current WebView content. Introduce `IScreenshotAdapter` as an optional facet interface on adapters.

## Scope
- Define `IScreenshotAdapter` with `Task<byte[]> CaptureScreenshotAsync()`
- Add `CaptureScreenshotAsync()` to `IWebView`
- Implement in all 5 platform adapters using native snapshot APIs
- Wire through `WebViewCore` with null-check for adapter support
- Add contract tests
