## 1. Core Contracts

- [x] 1.1 Define `WebViewPermissionKind` enum in `WebViewContracts.cs`
- [x] 1.2 Define `PermissionState` enum in `WebViewContracts.cs`
- [x] 1.3 Define `PermissionRequestedEventArgs` in `WebViewContracts.cs`
- [x] 1.4 Add `event EventHandler<PermissionRequestedEventArgs>? PermissionRequested` to `IWebView`

## 2. Adapter Abstractions

- [x] 2.1 Define `IPermissionAdapter` interface with `PermissionRequested` event

## 3. Runtime — WebViewCore

- [x] 3.1 Add `PermissionRequested` event to `WebViewCore`
- [x] 3.2 Detect `IPermissionAdapter` on adapter and subscribe to its `PermissionRequested`
- [x] 3.3 Forward adapter `PermissionRequested` to consumers on UI thread

## 4. Windows Adapter

- [x] 4.1 Implement `IPermissionAdapter`
- [x] 4.2 Subscribe to `CoreWebView2.PermissionRequested`
- [x] 4.3 Map WebView2 permission types to `WebViewPermissionKind`
- [x] 4.4 Apply consumer `PermissionState` to WebView2 response

## 5. macOS Adapter

- [x] 5.1 Implement `IPermissionAdapter`
- [x] 5.2 Handle via native shim: `ShimUIDelegate.requestMediaCapturePermissionForOrigin:` (macOS 12.0+)
- [x] 5.3 Map native permission types to `WebViewPermissionKind`

## 6. iOS Adapter

- [x] 6.1 Implement `IPermissionAdapter`
- [x] 6.2 Handle WKUIDelegate `requestMediaCapturePermissionForOrigin:` (iOS 15.0+)
- [x] 6.3 Map native permission types

## 7. GTK Adapter

- [x] 7.1 Implement `IPermissionAdapter`
- [x] 7.2 Handle `permission-request` signal on WebKitWebView
- [x] 7.3 Map WebKitPermissionRequest types to `WebViewPermissionKind`

## 8. Android Adapter

- [x] 8.1 Implement `IPermissionAdapter`
- [x] 8.2 Override `WebChromeClient.onPermissionRequest()`
- [x] 8.3 Map Android resource types to `WebViewPermissionKind`
- [x] 8.4 Check Android runtime permissions (`ContextCompat.CheckSelfPermission`) before granting WebView permissions

## 9. WebView Control + WebDialog

- [x] 9.1 Add `PermissionRequested` event to `WebView` control and wire subscription
- [x] 9.2 Add `PermissionRequested` event to `WebDialog` and `AvaloniaWebDialog`

## 10. Tests

- [x] 10.1 Unit tests for enums and `PermissionRequestedEventArgs`
- [x] 10.2 Contract tests: `IPermissionAdapter` detection and event forwarding
- [x] 10.3 Contract tests: Allow/Deny/Default propagation
- [x] 10.4 Update `MockWebViewAdapter` with `IPermissionAdapter` support
- [x] 10.5 Update `TestWebViewHost` with `PermissionRequested` event
