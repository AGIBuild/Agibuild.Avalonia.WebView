# Design: Download Management

## Architecture

```
Consumer
  │  WebView.DownloadRequested += (s, e) => {
  │      e.DownloadPath = "/path/to/save/file.pdf";
  │      // or e.Cancel = true;
  │  };
  │
  ▼
WebView / WebViewCore
  │  Raises DownloadRequestedEventArgs on UI thread
  │  Reads handler decision (DownloadPath / Cancel)
  │
  ▼
IWebViewAdapter (+ new IDownloadAdapter facet)
  │  Applies consumer decision to native download
  │
  ▼
Platform-native API
  Windows: CoreWebView2.DownloadStarting → set ResultFilePath / Cancel
  macOS: WKDownloadDelegate → decideDestination / cancel
  iOS: WKDownloadDelegate → decideDestination / cancel
  GTK: WebKitDownload → set destination / cancel
  Android: DownloadListener.onDownloadStart() → DownloadManager or custom
```

## Key Decisions

### D1: Download adapter as optional facet
`IDownloadAdapter` is an optional interface adapters MAY implement. Adapters that implement it receive download events from the native WebView and raise them through the adapter event.

### D2: Single event model (v1)
v1 uses a single `DownloadRequested` event. The consumer can:
- Set `DownloadPath` to allow and specify save location
- Set `Cancel = true` to deny the download
- Leave both unset → platform default behavior (usually save to default downloads folder)

Progress tracking and completion events are deferred to v2.

### D3: Event args design

```csharp
public sealed class DownloadRequestedEventArgs : EventArgs
{
    public Uri DownloadUri { get; init; }
    public string? SuggestedFileName { get; init; }
    public string? ContentType { get; init; }
    public long? ContentLength { get; init; }

    // Consumer decision
    public string? DownloadPath { get; set; }
    public bool Cancel { get; set; }
    public bool Handled { get; set; }
}
```

### D4: Thread model
`DownloadRequested` is raised synchronously on the UI thread. The consumer must set properties synchronously. Platform adapters that receive events on background threads marshal to UI thread.

### D5: Android special case
Android `DownloadListener.onDownloadStart()` does not provide built-in download — the app must handle it (e.g., via `DownloadManager` or `HttpClient`). The adapter raises the event; if `Handled` is not set, the adapter attempts to use Android's `DownloadManager` with the consumer's `DownloadPath` or system default.
