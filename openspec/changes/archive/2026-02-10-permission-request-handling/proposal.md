# Permission Request Handling

## Problem
When web content requests permissions (camera, microphone, geolocation, notifications), the WebView either auto-denies or shows a platform-specific dialog that the host application cannot control. For web-first hybrid apps, the host must be able to intercept, approve, deny, or defer permission requests programmatically.

## Proposed Solution
1. Define `WebViewPermissionKind` enum and `PermissionRequestedEventArgs` in Core
2. Add `PermissionRequested` event to `IWebView` and adapter contracts
3. Consumer controls permission: set `State` to Allow/Deny/Default
4. Implement in all 5 adapters using native APIs:
   - **Windows**: `CoreWebView2.PermissionRequested` event
   - **macOS**: `WKUIDelegate.webView:requestMediaCapturePermission:` (macOS 12+)
   - **iOS**: `WKUIDelegate.webView:requestMediaCapturePermission:` (iOS 15+)
   - **GTK**: `WebKitPermissionRequest` / `permission-request` signal
   - **Android**: `WebChromeClient.onPermissionRequest()`

## Scope
- `PermissionRequested` event with Allow/Deny/Default control
- Permission kinds: Camera, Microphone, Geolocation, Notifications, ClipboardRead
- Sync handler model (consumer sets state synchronously)
- Contract tests and E2E test scenario

## Out of Scope
- Persistent permission storage across sessions
- Permission policy / Feature-Policy header interception
- Combined permission requests (camera + microphone as single event)
