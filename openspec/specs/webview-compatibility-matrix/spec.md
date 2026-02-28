## Purpose
Define compatibility-matrix governance contracts and acceptance-criteria traceability.
## Requirements
### Requirement: Compatibility matrix exists and is versioned
The repository SHALL maintain a versioned WebView compatibility matrix that defines:
- supported platforms (Windows, macOS/iOS, Android, Linux)
- supported modes (Embedded, Dialog, Auth)
- capability coverage by support level (Baseline vs Extended vs Not Supported)
- acceptance criteria per capability (CT and/or IT requirements)

#### Scenario: Matrix document is present
- **WHEN** a contributor inspects the repository documentation
- **THEN** a compatibility matrix can be found and includes platforms, modes, support levels, and acceptance criteria

### Requirement: Baseline capabilities are falsifiable
For each Baseline capability listed in the matrix, the matrix SHALL identify at least one deterministic Contract Test (CT) scenario that validates the baseline semantics.

#### Scenario: Each baseline capability maps to CT
- **WHEN** a capability is marked as Baseline in the matrix
- **THEN** the matrix includes CT acceptance criteria for that capability

### Requirement: Extended capabilities document platform differences
For each capability marked as Extended with platform differences, the repository SHALL document the difference using a standard "platform difference" entry including:
- affected platforms/modes
- user-visible behavior
- security implications (if any)
- test impact (CT conditionalization or IT substitution)

#### Scenario: Platform differences are documented
- **WHEN** a capability is marked as Extended with a platform warning
- **THEN** a platform difference entry exists with behavior, security implications, and test impact

### Requirement: Linux embedded mode is explicitly out of baseline scope
The matrix SHALL explicitly state that Linux Embedded mode is not part of Baseline support and that Linux support is provided via Dialog mode only.

#### Scenario: Linux embedded is not promised
- **WHEN** the matrix is reviewed for Linux support
- **THEN** it does not claim Baseline support for Embedded mode on Linux

### Requirement: Compatibility matrix records macOS WKWebView M0 acceptance criteria
The compatibility matrix SHALL include an entry for macOS (WKWebView) that documents M0 coverage for Embedded mode navigation and minimal script/message-bridge behavior.

The matrix entry SHALL identify acceptance criteria using both CT and IT as applicable:
- CT: contract semantics scenarios that are platform-independent and deterministic
- IT: macOS-only smoke scenarios that validate WKWebView behavior for native navigation interception/correlation

#### Scenario: Matrix includes macOS WKWebView navigation acceptance criteria
- **WHEN** a contributor inspects the compatibility matrix for macOS (WKWebView) Embedded mode
- **THEN** it lists acceptance criteria covering link click, 302 redirect correlation, `window.location`, and cancellation (`Cancel=true`)

#### Scenario: Matrix includes macOS WKWebView minimal script/bridge acceptance criteria
- **WHEN** a contributor inspects the compatibility matrix for macOS (WKWebView) Embedded mode
- **THEN** it lists acceptance criteria covering minimal script execution and WebMessage bridge receive behavior

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

### Requirement: Command Manager capability in matrix
The compatibility matrix SHALL list command manager as an Extended capability with:
- All platforms (Windows, macOS, iOS, Android, GTK): supported via `ICommandAdapter` facet
- Commands: Copy, Cut, Paste, SelectAll, Undo, Redo
- CT: contract tests via mock adapter verifying facet detection and command delegation

#### Scenario: Command Manager is listed in matrix
- **WHEN** the matrix is reviewed for command manager
- **THEN** it shows all platforms as supported with CT acceptance criteria

### Requirement: Screenshot capture capability in matrix
The compatibility matrix SHALL list screenshot capture as an Extended capability with:
- All platforms (Windows, macOS, iOS, Android, GTK): supported via `IScreenshotAdapter` facet
- Returns PNG bytes; throws `NotSupportedException` when adapter lacks support
- CT: contract tests via mock adapter verifying PNG header and unsupported behavior

#### Scenario: Screenshot capture is listed in matrix
- **WHEN** the matrix is reviewed for screenshot capture
- **THEN** it shows all platforms as supported with CT acceptance criteria

### Requirement: Print to PDF capability in matrix
The compatibility matrix SHALL list print-to-pdf as an Extended capability with:
- Windows (WebView2): supported via `CoreWebView2.PrintToPdfAsync`
- macOS/iOS (WKWebView): supported via native PDF rendering
- Android: not supported (throws `NotSupportedException`)
- GTK/Linux: not supported — WebKitGTK lacks a PDF export API
- CT: contract tests via mock adapter verifying PDF header, options pass-through, and unsupported behavior

#### Scenario: Print to PDF is listed in matrix
- **WHEN** the matrix is reviewed for print-to-pdf
- **THEN** it shows Windows/macOS/iOS as supported and Android/GTK as not supported

### Requirement: JS-C# RPC capability in matrix
The compatibility matrix SHALL list JS ↔ C# RPC as an Extended capability with:
- All platforms: supported when WebMessage bridge is enabled
- Protocol: JSON-RPC 2.0 over the WebMessage bridge
- Requires explicit bridge enable (`EnableWebMessageBridge`)
- CT: contract tests via mock adapter verifying handler registration, invocation, error handling

#### Scenario: RPC is listed in matrix
- **WHEN** the matrix is reviewed for JS ↔ C# RPC
- **THEN** it shows all platforms as supported with CT acceptance criteria and bridge dependency noted

### Requirement: Compatibility matrix SHALL remain synchronized with executable evidence manifests
Compatibility matrix capability entries SHALL map to executable evidence present in runtime automation or contract manifests.

#### Scenario: Matrix entry lacks executable evidence mapping
- **WHEN** governance validation scans matrix capability entries
- **THEN** validation fails if a capability has no linked executable test evidence

#### Scenario: Manifest references capability missing from matrix
- **WHEN** runtime-critical manifest contains a governed capability id
- **THEN** matrix includes the same capability id with platform coverage details

### Requirement: Platform parity claims SHALL be machine-checkable
Platform coverage claims in matrix rows SHALL include deterministic coverage tokens for each declared platform.

#### Scenario: Declared platform has empty coverage token list
- **WHEN** matrix governance checks platform coverage payload
- **THEN** validation fails with capability id and platform name

### Requirement: Compatibility matrix documents macOS DevTools toggle limitation
The compatibility matrix SHALL mark macOS OpenDevTools/CloseDevTools as ⚠️ (No-op) with a note that the Web Inspector is available via right-click when EnableDevTools is set.

#### Scenario: Matrix shows macOS DevTools toggle status
- **WHEN** a developer consults the compatibility matrix for DevTools
- **THEN** macOS shows ⚠️ with note about right-click access

### Requirement: Compatibility matrix documents IAsyncPreloadScriptAdapter as Windows-only
The compatibility matrix SHALL include an IAsyncPreloadScriptAdapter row marking Windows as ✅ and all other platforms as ❌ (fallback to sync IPreloadScriptAdapter).

#### Scenario: Matrix shows async preload cross-platform status
- **WHEN** a developer consults the compatibility matrix for async preload
- **THEN** only Windows is marked ✅, others show ❌ with fallback note

### Requirement: Compatibility declarations SHALL distinguish unsupported from undeclared
Platform compatibility declarations SHALL explicitly mark unsupported platform scope with deterministic token (`n/a`) instead of omitting platform keys.

#### Scenario: Unsupported mobile scope is explicitly declared
- **WHEN** compatibility/governance artifacts are generated for shell capabilities
- **THEN** iOS/Android scope is explicitly represented as `n/a` where executable evidence is not yet available

