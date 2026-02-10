# Web Resource Interception — Cross-Platform

## Problem
`WebResourceRequested` event exists in `IWebViewAdapter` and `IWebView` but all 5 adapters stub it (`add {} remove {}`). Windows uses it internally for `NavigateToStringAsync(html, baseUrl)` but never raises it to consumers. Users cannot intercept network requests, register custom URI schemes, or serve local content — essential for Electron-replacement scenarios.

## Proposed Solution
1. Add a **custom scheme registration API** to `IWebViewEnvironmentOptions` (schemes must be registered before WebView creation)
2. Enrich `WebResourceRequestedEventArgs` with request headers and binary response support (`Stream`)
3. Implement the event in all 5 platform adapters using native APIs:
   - **Windows**: `CoreWebView2CustomSchemeRegistration` + `AddWebResourceRequestedFilter`
   - **macOS/iOS**: `WKURLSchemeHandler` via native shim
   - **GTK**: `webkit_web_context_register_uri_scheme()`
   - **Android**: `WebViewClient.shouldInterceptRequest()`
4. Keep HTTP/HTTPS interception opt-in and platform-limited (only Windows/Android can intercept standard schemes)

## Scope
- Custom scheme registration and interception on all 5 platforms
- Standard HTTP/HTTPS interception on Windows and Android (opt-in)
- Enriched event args (request headers, Stream response body)
- Contract tests for all new behavior
- E2E test scenario with custom scheme

## Out of Scope
- Service Worker / offline cache
- Request body modification (POST interception)
- Response header interception (only custom responses)
