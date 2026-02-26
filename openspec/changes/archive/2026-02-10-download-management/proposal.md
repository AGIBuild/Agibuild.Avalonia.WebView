# Download Management

## Problem
When a user clicks a download link or the server responds with `Content-Disposition: attachment`, the WebView silently drops the download. There is no way for the host application to intercept, save, or track file downloads — a critical gap for web-first hybrid scenarios.

## Proposed Solution
1. Define `DownloadRequestedEventArgs` in Core with download metadata (URI, filename, content type, content length)
2. Add `DownloadRequested` event to `IWebView` and `IWebViewAdapter`
3. Consumer controls download: set `DownloadPath` to save, or set `Cancel = true` to deny
4. Implement in all 5 platform adapters using native download APIs:
   - **Windows**: `CoreWebView2.DownloadStarting` event
   - **macOS**: `WKDownloadDelegate` (macOS 11.3+) / `decidePolicyForNavigationResponse`
   - **iOS**: `WKDownloadDelegate` (iOS 14.5+)
   - **GTK**: `WebKitDownload` signal on `WebKitWebView`
   - **Android**: `DownloadListener.onDownloadStart()`

## Scope
- `DownloadRequested` event with allow/deny/redirect control
- Download path selection by consumer
- Basic download metadata (URI, filename, size, content type)
- Contract tests and E2E test scenario

## Out of Scope
- Progress tracking (v2)
- Resume / pause downloads (v2)
- Download manager UI
- Background downloads
