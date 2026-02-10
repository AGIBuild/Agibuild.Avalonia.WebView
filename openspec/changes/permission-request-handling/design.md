# Design: Permission Request Handling

## Architecture

```
Consumer
  │  WebView.PermissionRequested += (s, e) => {
  │      if (e.PermissionKind == WebViewPermissionKind.Camera)
  │          e.State = PermissionState.Allow;
  │      else
  │          e.State = PermissionState.Deny;
  │  };
  │
  ▼
WebView / WebViewCore
  │  Raises PermissionRequestedEventArgs on UI thread
  │  Reads handler decision (State)
  │
  ▼
IWebViewAdapter (+ new IPermissionAdapter facet)
  │  Applies consumer decision to native permission
  │
  ▼
Platform-native API
  Windows: CoreWebView2.PermissionRequested → set State
  macOS: WKUIDelegate.requestMediaCapturePermission → grant/deny
  iOS: WKUIDelegate.requestMediaCapturePermission → grant/deny
  GTK: permission-request signal → allow/deny
  Android: WebChromeClient.onPermissionRequest → grant/deny
```

## Key Decisions

### D1: Permission adapter as optional facet
`IPermissionAdapter` is an optional interface adapters MAY implement. Similar pattern to `ICookieAdapter`, `ICustomSchemeAdapter`, `IDownloadAdapter`.

### D2: Permission kinds enum
```csharp
public enum WebViewPermissionKind
{
    Unknown = 0,
    Camera,
    Microphone,
    Geolocation,
    Notifications,
    ClipboardRead,
    ClipboardWrite,
    Midi,
    Sensors,
    Other
}
```

### D3: Permission state enum
```csharp
public enum PermissionState
{
    Default = 0,  // Let the platform handle it (show native dialog)
    Allow,
    Deny
}
```

### D4: Event args design
```csharp
public sealed class PermissionRequestedEventArgs : EventArgs
{
    public WebViewPermissionKind PermissionKind { get; init; }
    public Uri? Origin { get; init; }
    public PermissionState State { get; set; } = PermissionState.Default;
}
```

### D5: Thread model
`PermissionRequested` is raised synchronously on the UI thread.
On platforms where the permission callback is async (macOS/iOS), the adapter defers the response until the UI-thread handler completes.

### D6: Platform permission mapping
| Platform permission | WebViewPermissionKind |
|--------------------|-----------------------|
| WebView2 `Camera` | Camera |
| WebView2 `Microphone` | Microphone |
| WebView2 `Geolocation` | Geolocation |
| WebView2 `Notifications` | Notifications |
| WebView2 `ClipboardRead` | ClipboardRead |
| WKWebView `mediaCapture.camera` | Camera |
| WKWebView `mediaCapture.microphone` | Microphone |
| Android `RESOURCE_VIDEO_CAPTURE` | Camera |
| Android `RESOURCE_AUDIO_CAPTURE` | Microphone |
| WebKitGTK `WEBKIT_PERMISSION_REQUEST` | mapped per request type |
