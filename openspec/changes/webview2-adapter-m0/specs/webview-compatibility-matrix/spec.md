## ADDED Requirements

### Requirement: Windows WebView2 M0 acceptance criteria
The compatibility matrix SHALL record Windows/WebView2 M0 acceptance criteria covering:
- Navigation interception: CT for full-control navigation semantics; IT for link click, redirect correlation, `window.location`, cancellation on Windows
- Script execution: CT for `InvokeScriptAsync` result/failure mapping; IT for `ExecuteScriptAsync` round-trip on Windows
- WebMessage bridge: CT for bridge policy gating; IT for `WebMessageReceived` delivery on Windows
- Cookie management: CT for `ICookieManager` CRUD via mock adapter; IT for `CoreWebView2CookieManager` set/get/delete on Windows
- Error categorization: CT for `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException` mapping; IT for real network error on Windows
- Native handle provider: IT for `TryGetWebViewHandle()` returning a valid handle with descriptor `"WebView2"` on Windows
- NavigateToStringAsync baseUrl: CT for `Source` and `RequestUri` semantics; IT for relative resource resolution on Windows
- NewWindowRequested: CT for unhandled fallback semantics; IT for `window.open` event propagation on Windows

#### Scenario: Windows WebView2 M0 criteria are recorded
- **WHEN** a contributor inspects the compatibility matrix for Windows/WebView2 M0
- **THEN** acceptance criteria for navigation, scripting, bridge, cookies, error categorization, native handle, baseUrl, and new-window are listed

### Requirement: Windows WebView2 cookie management in matrix
The compatibility matrix SHALL update the cookie management capability entry to include:
- Windows/WebView2: supported via `CoreWebView2CookieManager` (M0)

#### Scenario: Cookie management shows Windows as supported
- **WHEN** the matrix is reviewed for cookie management
- **THEN** it shows both macOS and Windows as supported

### Requirement: Windows WebView2 error categorization in matrix
The compatibility matrix SHALL update the error categorization capability entry to include:
- Windows/WebView2: maps `CoreWebView2WebErrorStatus` to `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`

#### Scenario: Error categorization shows Windows mapping
- **WHEN** the matrix is reviewed for error categorization
- **THEN** it shows both macOS (`NSURLError`) and Windows (`CoreWebView2WebErrorStatus`) mappings
