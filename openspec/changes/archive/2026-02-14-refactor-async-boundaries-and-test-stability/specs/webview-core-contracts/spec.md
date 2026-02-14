## MODIFIED Requirements

### Requirement: Environment options and native handles
The Core assembly SHALL define:
- `IWebViewEnvironmentOptions` with `bool EnableDevTools { get; set; }`, `IReadOnlyList<CustomSchemeRegistration>? CustomSchemes { get; set; }`
- `INativeWebViewHandleProvider` with `IPlatformHandle? TryGetWebViewHandle()`
- `IWindowsWebView2PlatformHandle` extending `IPlatformHandle` with `nint CoreWebView2Handle` and `nint CoreWebView2ControllerHandle`
- `IAppleWKWebViewPlatformHandle` extending `IPlatformHandle` with `nint WKWebViewHandle`
- `IGtkWebViewPlatformHandle` extending `IPlatformHandle` with `nint WebKitWebViewHandle`
- `IAndroidWebViewPlatformHandle` extending `IPlatformHandle` with `nint AndroidWebViewHandle`
- `IWebView` async native-handle accessor `Task<IPlatformHandle?> TryGetWebViewHandleAsync()`

The runtime SHALL treat async native-handle retrieval as the primary contract path.  
Any synchronous retrieval path SHALL be explicitly documented as a compatibility boundary and SHALL NOT introduce hidden global state mutations.

#### Scenario: Async native handle retrieval is contract-visible
- **WHEN** a consumer references `IWebView.TryGetWebViewHandleAsync()`
- **THEN** the contract compiles and returns `Task<IPlatformHandle?>`

#### Scenario: Async and sync retrieval observe same lifecycle outcome
- **WHEN** adapter lifecycle reaches destroyed state
- **THEN** both async and sync retrieval paths resolve to `null`
