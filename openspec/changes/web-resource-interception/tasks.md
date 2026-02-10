## 1. Core Contracts

- [x] 1.1 Define `CustomSchemeRegistration` class in `WebViewContracts.cs`
- [x] 1.2 Add `IReadOnlyList<CustomSchemeRegistration> CustomSchemes` to `IWebViewEnvironmentOptions`
- [x] 1.3 Add `CustomSchemes` property to `WebViewEnvironmentOptions` with empty list default
- [x] 1.4 Update `WebResourceRequestedEventArgs`: replace `string? ResponseBody` with `Stream? ResponseBody`, add `RequestHeaders`, `ResponseHeaders`

## 2. Adapter Abstractions

- [x] 2.1 Define `ICustomSchemeAdapter` interface with `RegisterCustomSchemes(IReadOnlyList<CustomSchemeRegistration>)`

## 3. Runtime — WebViewCore

- [x] 3.1 Detect `ICustomSchemeAdapter` on adapter and call `RegisterCustomSchemes()` before `Attach()`
- [x] 3.2 Ensure `WebResourceRequested` from adapter is forwarded to consumers on UI thread

## 4. Windows Adapter

- [x] 4.1 Implement `ICustomSchemeAdapter` — register custom schemes via `CoreWebView2CustomSchemeRegistration`
- [x] 4.2 Update `OnWebResourceRequested` to raise public `WebResourceRequested` event for custom scheme requests
- [x] 4.3 Convert `Stream` response to `CoreWebView2WebResourceResponse`

## 5. macOS Adapter

- [x] 5.1 Implement `ICustomSchemeAdapter` — register schemes via `WKURLSchemeHandler` in native shim
- [x] 5.2 Add native `ShimSchemeHandler` (WKURLSchemeHandler) and `ag_wk_register_custom_scheme` API
- [x] 5.3 Raise `WebResourceRequested` from scheme handler callback via `ag_wk_scheme_request_cb`

## 6. iOS Adapter

- [x] 6.1 Implement `ICustomSchemeAdapter` — register schemes via `WKURLSchemeHandler`
- [x] 6.2 Add native `ShimSchemeHandler` and `ag_wk_register_custom_scheme` for iOS
- [x] 6.3 Raise `WebResourceRequested` from scheme handler callback

## 7. GTK Adapter

- [x] 7.1 Implement `ICustomSchemeAdapter` — register via `webkit_web_context_register_uri_scheme()`
- [x] 7.2 Raise `WebResourceRequested` from `on_custom_scheme_request` callback

## 8. Android Adapter

- [x] 8.1 Implement `ICustomSchemeAdapter` — intercept in `shouldInterceptRequest()`
- [x] 8.2 Raise `WebResourceRequested` for registered custom schemes via `WebViewClient.ShouldInterceptRequest`
- [x] 8.3 Convert `Stream` response to `WebResourceResponse`

## 9. WebView Control

- [x] 9.1 Ensure `WebResourceRequested` event is already bubbled (verify existing wiring)

## 10. Tests

- [x] 10.1 Unit tests for `CustomSchemeRegistration` and updated `WebResourceRequestedEventArgs`
- [x] 10.2 Contract tests: `ICustomSchemeAdapter` detection and `RegisterCustomSchemes()` call ordering
- [x] 10.3 Contract tests: `WebResourceRequested` forwarding with `Handled = true/false`
- [x] 10.4 Update `MockWebViewAdapter` to support `ICustomSchemeAdapter`
