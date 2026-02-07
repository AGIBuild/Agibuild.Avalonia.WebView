## ADDED Requirements

### Requirement: Android WebView M0 acceptance criteria
The compatibility matrix SHALL record Android/WebView M0 acceptance criteria covering:
- Navigation interception: CT for full-control navigation semantics; IT for link click, redirect correlation, `window.location`, cancellation on Android
- Script execution: CT for `InvokeScriptAsync` result/failure mapping; IT for `evaluateJavascript` round-trip on Android
- WebMessage bridge: CT for bridge policy gating; IT for `WebMessageReceived` delivery on Android
- Cookie management: CT for `ICookieManager` CRUD via mock adapter; IT for `android.webkit.CookieManager` set/get/delete on Android
- Error categorization: CT for `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException` mapping; IT for real network error on Android
- Native handle provider: IT for `TryGetWebViewHandle()` returning a valid handle with descriptor `"AndroidWebView"` on Android
- NavigateToStringAsync baseUrl: CT for `Source` and `RequestUri` semantics; IT for `loadDataWithBaseURL` relative resource resolution on Android
- NewWindowRequested: CT for unhandled fallback semantics; IT for `window.open` event propagation on Android

#### Scenario: Android WebView M0 criteria are recorded
- **WHEN** a contributor inspects the compatibility matrix for Android/WebView M0
- **THEN** acceptance criteria for navigation, scripting, bridge, cookies, error categorization, native handle, baseUrl, and new-window are listed

### Requirement: Android WebView cookie management in matrix
The compatibility matrix SHALL update the cookie management capability entry to include:
- Android/WebView: supported via `android.webkit.CookieManager` (M0)

#### Scenario: Cookie management shows Android as supported
- **WHEN** the matrix is reviewed for cookie management
- **THEN** it shows macOS, Windows, and Android as supported

### Requirement: Android WebView error categorization in matrix
The compatibility matrix SHALL update the error categorization capability entry to include:
- Android/WebView: maps `WebViewClient.ERROR_*` codes to `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`

#### Scenario: Error categorization shows Android mapping
- **WHEN** the matrix is reviewed for error categorization
- **THEN** it shows macOS (`NSURLError`), Windows (`CoreWebView2WebErrorStatus`), and Android (`WebViewClient.ERROR_*`) mappings
