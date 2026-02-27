## MODIFIED Requirements

### Requirement: Typed Windows WebView2 platform handle interface
The Core assembly SHALL define `IWindowsWebView2PlatformHandle` extending `INativeHandle` with:
- `nint CoreWebView2Handle { get; }` — pointer to the `ICoreWebView2` COM object
- `nint CoreWebView2ControllerHandle { get; }` — pointer to the `ICoreWebView2Controller` COM object

#### Scenario: Windows adapter returns typed handle
- **WHEN** the Windows adapter's `TryGetWebViewHandle()` is called after Attach
- **THEN** the returned handle implements `IWindowsWebView2PlatformHandle`
- **AND** `HandleDescriptor` is `"WebView2"`
- **AND** `CoreWebView2Handle` and `CoreWebView2ControllerHandle` are non-zero

#### Scenario: Consumer pattern-matches Windows handle
- **WHEN** a consumer casts the `INativeHandle` to `IWindowsWebView2PlatformHandle`
- **THEN** they access `CoreWebView2Handle` and `CoreWebView2ControllerHandle` without referencing the Windows adapter assembly

### Requirement: Typed Apple WKWebView platform handle interface
The Core assembly SHALL define `IAppleWKWebViewPlatformHandle` extending `INativeHandle` with:
- `nint WKWebViewHandle { get; }` — Objective-C pointer to the `WKWebView` instance

This interface SHALL be used by both macOS and iOS adapters.

#### Scenario: macOS adapter returns typed handle
- **WHEN** the macOS adapter's `TryGetWebViewHandle()` is called after Attach
- **THEN** the returned handle implements `IAppleWKWebViewPlatformHandle`
- **AND** `HandleDescriptor` is `"WKWebView"`
- **AND** `WKWebViewHandle` is non-zero

#### Scenario: iOS adapter returns typed handle
- **WHEN** the iOS adapter's `TryGetWebViewHandle()` is called after Attach
- **THEN** the returned handle implements `IAppleWKWebViewPlatformHandle`
- **AND** `HandleDescriptor` is `"WKWebView"`
- **AND** `WKWebViewHandle` is non-zero

### Requirement: Typed GTK WebKit platform handle interface
The Core assembly SHALL define `IGtkWebViewPlatformHandle` extending `INativeHandle` with:
- `nint WebKitWebViewHandle { get; }` — pointer to the `WebKitWebView` GObject instance

#### Scenario: GTK adapter returns typed handle
- **WHEN** the GTK adapter's `TryGetWebViewHandle()` is called after Attach
- **THEN** the returned handle implements `IGtkWebViewPlatformHandle`
- **AND** `HandleDescriptor` is `"WebKitGTK"`
- **AND** `WebKitWebViewHandle` is non-zero

### Requirement: Typed Android WebView platform handle interface
The Core assembly SHALL define `IAndroidWebViewPlatformHandle` extending `INativeHandle` with:
- `nint AndroidWebViewHandle { get; }` — JNI handle to the Android `WebView` instance

#### Scenario: Android adapter returns typed handle
- **WHEN** the Android adapter's `TryGetWebViewHandle()` is called after Attach
- **THEN** the returned handle implements `IAndroidWebViewPlatformHandle`
- **AND** `HandleDescriptor` is `"AndroidWebView"`
- **AND** `AndroidWebViewHandle` is non-zero

### Requirement: Typed handle interfaces are platform-agnostic in Core
All typed platform handle interfaces SHALL be defined in `Agibuild.Fulora.Core` without any platform-specific dependencies.
They SHALL use only `nint` for native pointers, avoiding any platform SDK types.

#### Scenario: Core compiles without platform dependencies
- **WHEN** a project references `Agibuild.Fulora.Core` on any platform
- **THEN** all typed handle interfaces are resolvable without platform-specific NuGet packages or TFM restrictions
