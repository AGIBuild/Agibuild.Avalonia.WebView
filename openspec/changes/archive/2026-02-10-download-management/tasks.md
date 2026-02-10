## 1. Core Contracts

- [x] 1.1 Define `DownloadRequestedEventArgs` in `WebViewContracts.cs`
- [x] 1.2 Add `event EventHandler<DownloadRequestedEventArgs>? DownloadRequested` to `IWebView`

## 2. Adapter Abstractions

- [x] 2.1 Define `IDownloadAdapter` interface with `DownloadRequested` event

## 3. Runtime — WebViewCore

- [x] 3.1 Add `DownloadRequested` event to `WebViewCore`
- [x] 3.2 Detect `IDownloadAdapter` on adapter and subscribe to its `DownloadRequested`
- [x] 3.3 Forward adapter `DownloadRequested` to consumers on UI thread

## 4. Windows Adapter

- [x] 4.1 Implement `IDownloadAdapter`
- [x] 4.2 Subscribe to `CoreWebView2.DownloadStarting`
- [x] 4.3 Map `DownloadStarting` → `DownloadRequestedEventArgs`, apply consumer decisions

## 5. macOS Adapter

- [x] 5.1 Implement `IDownloadAdapter`
- [x] 5.2 Handle downloads via native shim (WKNavigationDelegate `decidePolicyForNavigationResponse:`)
- [x] 5.3 Raise `DownloadRequested` with metadata from native callback

## 6. iOS Adapter

- [x] 6.1 Implement `IDownloadAdapter`
- [x] 6.2 Handle downloads via WKNavigationDelegate `decidePolicyForNavigationResponse:`
- [x] 6.3 Raise `DownloadRequested`

## 7. GTK Adapter

- [x] 7.1 Implement `IDownloadAdapter`
- [x] 7.2 Handle `download-started` signal on WebKitWebContext
- [x] 7.3 Raise `DownloadRequested` with download metadata

## 8. Android Adapter

- [x] 8.1 Implement `IDownloadAdapter`
- [x] 8.2 Set `DownloadListener` on Android WebView
- [x] 8.3 Raise `DownloadRequested` from `onDownloadStart`
- [x] 8.4 Use `DownloadManager` for unhandled downloads

## 9. WebView Control + WebDialog

- [x] 9.1 Add `DownloadRequested` event to `WebView` control and wire subscription
- [x] 9.2 Add `DownloadRequested` event to `WebDialog` and `AvaloniaWebDialog`

## 10. Tests

- [x] 10.1 Unit tests for `DownloadRequestedEventArgs` construction
- [x] 10.2 Contract tests: `IDownloadAdapter` detection and event forwarding
- [x] 10.3 Contract tests: Cancel/DownloadPath/Handled propagation
- [x] 10.4 Update `MockWebViewAdapter` with `IDownloadAdapter` support
- [x] 10.5 Update `TestWebViewHost` with `DownloadRequested` event
