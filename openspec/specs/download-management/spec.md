## Purpose
Define download management contracts for events, adapter facets, and consumer control.

## Requirements

### Requirement: DownloadRequestedEventArgs type in Core
The Core assembly SHALL define `DownloadRequestedEventArgs : EventArgs` with:
- `Uri DownloadUri { get; }` — the download URL
- `string? SuggestedFileName { get; }` — suggested filename from Content-Disposition or URL
- `string? ContentType { get; }` — MIME type of the download
- `long? ContentLength { get; }` — content length in bytes, null if unknown
- `string? DownloadPath { get; set; }` — consumer sets save path
- `bool Cancel { get; set; }` — consumer sets true to deny download
- `bool Handled { get; set; }` — consumer sets true when fully handling the download

#### Scenario: Event args carry download metadata
- **WHEN** a download is initiated by a link with `Content-Disposition: attachment; filename="report.pdf"`
- **THEN** `DownloadRequested` is raised with `SuggestedFileName == "report.pdf"`

### Requirement: IWebView includes DownloadRequested event
The `IWebView` interface SHALL define:
- `event EventHandler<DownloadRequestedEventArgs>? DownloadRequested`

#### Scenario: DownloadRequested event is available on IWebView
- **WHEN** a consumer subscribes to `IWebView.DownloadRequested`
- **THEN** the subscription compiles without error

### Requirement: IDownloadAdapter facet for adapters
The adapter abstractions SHALL define `IDownloadAdapter`:
- `event EventHandler<DownloadRequestedEventArgs>? DownloadRequested`

Adapters MAY implement `IDownloadAdapter` alongside `IWebViewAdapter`.
The runtime SHALL detect `IDownloadAdapter` support via type check and subscribe to its events.

#### Scenario: Adapter implementing IDownloadAdapter enables download events
- **WHEN** an adapter implements `IDownloadAdapter`
- **THEN** `WebViewCore` subscribes to `DownloadRequested` and forwards to consumers

#### Scenario: Adapter without IDownloadAdapter silently skips
- **WHEN** an adapter does NOT implement `IDownloadAdapter`
- **THEN** `DownloadRequested` is never raised and no error occurs

### Requirement: Consumer can control download via event args
The runtime SHALL honor consumer-controlled download outcomes when `DownloadRequested` is raised:
- If consumer sets `Cancel = true`, the download SHALL be canceled
- If consumer sets `DownloadPath`, the download SHALL save to that path
- If consumer sets `Handled = true`, the adapter SHALL not perform any default download action
- If nothing is set, platform default behavior applies

#### Scenario: Consumer cancels download
- **WHEN** the handler sets `Cancel = true`
- **THEN** the native download is canceled

#### Scenario: Consumer redirects download to custom path
- **WHEN** the handler sets `DownloadPath = "/tmp/myfile.pdf"`
- **THEN** the file is saved to `/tmp/myfile.pdf`

### Requirement: All platform adapters implement IDownloadAdapter
All five platform adapters (Windows, macOS, iOS, GTK, Android) SHALL implement `IDownloadAdapter` using the appropriate native API.

#### Scenario: Windows adapter handles downloads via DownloadStarting
- **WHEN** WebView2 triggers `DownloadStarting`
- **THEN** the adapter raises `DownloadRequested` and applies consumer decisions

#### Scenario: macOS adapter handles downloads via WKDownloadDelegate
- **WHEN** WKWebView initiates a download
- **THEN** the adapter raises `DownloadRequested`

#### Scenario: Android adapter handles downloads via DownloadListener
- **WHEN** Android WebView triggers `onDownloadStart`
- **THEN** the adapter raises `DownloadRequested`

### Requirement: WebView control bubbles DownloadRequested
The `WebView` Avalonia control SHALL subscribe to `WebViewCore`'s `DownloadRequested` and re-raise it.

#### Scenario: WebView.DownloadRequested fires
- **WHEN** the underlying WebViewCore raises DownloadRequested
- **THEN** the WebView control raises its own DownloadRequested with the same args
