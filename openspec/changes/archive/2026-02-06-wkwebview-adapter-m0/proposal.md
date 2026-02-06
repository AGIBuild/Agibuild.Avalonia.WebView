## Why

We need a real macOS (WKWebView) adapter to validate the v1 contract semantics against a native WebView and to unblock platform integration work.
This change delivers a first “M0” implementation focused on correct navigation interception/correlation and a minimal script/message-bridge loop.

## What Changes

- Implement `MacOSWebViewAdapter` (WKWebView-backed) to satisfy existing Core + Adapter Abstraction contracts for:
  - native-initiated navigation interception via `IWebViewAdapterHost.OnNativeNavigationStartingAsync(...)`
  - allow/deny decisions and cancellation behavior
  - redirect correlation: reuse the same `CorrelationId` and report `NavigationCompleted` using the host-issued `NavigationId`
- Add macOS integration-test (IT) smoke coverage for WKWebView:
  - link click navigation
  - 302 redirect navigation (same correlation chain)
  - `window.location` script-driven navigation
  - cancellation path (`Cancel=true` -> deny native step + completed as `Canceled`)
  - minimal script execution + WebMessage bridge round-trip
- Update the compatibility matrix acceptance criteria to reflect the macOS/WKWebView M0 coverage and its CT/IT mapping.

## Capabilities

### New Capabilities

- (none)

### Modified Capabilities

- `webview-testing-harness`: add explicit macOS/WKWebView IT smoke requirements and scenarios for native navigation + script/bridge minimal loop
- `webview-compatibility-matrix`: record macOS/WKWebView Embedded-mode M0 coverage and acceptance criteria (CT + IT)

## Impact

- Affected code: `Agibuild.Avalonia.WebView.Adapters.MacOS` (WKWebView adapter), integration test projects and test app harness for macOS.
- Public API surface: no intended breaking changes (contracts are already defined; this change implements them on macOS).
- Dependencies: macOS WebKit/WKWebView bindings as required by `net10.0-macos` build.
