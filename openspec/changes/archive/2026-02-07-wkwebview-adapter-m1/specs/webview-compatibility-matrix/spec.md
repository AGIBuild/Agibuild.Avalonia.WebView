## ADDED Requirements

### Requirement: macOS M1 acceptance criteria
The compatibility matrix SHALL record macOS/WKWebView M1 acceptance criteria covering:
- Cookie management: CT for `ICookieManager` CRUD via mock adapter; IT for `WKHTTPCookieStore` set/get/delete on macOS
- NewWindowRequested fallback: CT for unhandled → in-view navigation; CT for handled → no fallback
- Native handle provider: IT for `TryGetWebViewHandle()` returning a valid `WKWebView` handle on macOS
- Error categorization: CT for `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException` mapping; IT for real network error on macOS
- NavigateToStringAsync baseUrl: CT for `Source` and `RequestUri` semantics; IT for relative resource resolution on macOS

#### Scenario: macOS M1 criteria are recorded
- **WHEN** a contributor inspects the compatibility matrix for macOS M1
- **THEN** acceptance criteria for cookie management, new-window fallback, native handle, error categorization, and baseUrl are listed

### Requirement: Cookie management capability in matrix
The compatibility matrix SHALL list cookie management as an Extended capability with:
- macOS/WKWebView: supported via `WKHTTPCookieStore` (M1)
- Other platforms: not yet supported (null from `TryGetCookieManager()`)
- Platform difference: cookie store isolation model may differ (shared vs per-data-store)

#### Scenario: Cookie management is listed in matrix
- **WHEN** the matrix is reviewed for cookie management
- **THEN** it shows macOS as supported and other platforms as not yet supported

### Requirement: Error categorization capability in matrix
The compatibility matrix SHALL list navigation error categorization as a Baseline enhancement with:
- macOS/WKWebView: maps `NSURLError` codes to `WebViewNetworkException`, `WebViewSslException`, `WebViewTimeoutException`
- Platform difference: specific error codes mapped vary by platform; unmapped errors fall back to `WebViewNavigationException`

#### Scenario: Error categorization is listed in matrix
- **WHEN** the matrix is reviewed for error categorization
- **THEN** it shows the error hierarchy and platform-specific mapping notes
