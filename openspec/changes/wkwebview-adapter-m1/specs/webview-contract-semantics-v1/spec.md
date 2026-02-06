## ADDED Requirements

### Requirement: NewWindowRequested unhandled fallback navigation
When the runtime raises `NewWindowRequested` and no handler sets `Handled = true`, the runtime SHALL navigate to the requested URI in the current view if `Uri` is non-null.
If `Handled` is set to `true` by a handler, the runtime SHALL NOT perform any automatic navigation.
If `Uri` is `null` and `Handled` is `false`, the runtime SHALL take no action.

#### Scenario: Unhandled new window navigates in current view
- **WHEN** a `NewWindowRequested` event is raised with a non-null `Uri` and no handler sets `Handled = true`
- **THEN** the runtime calls `NavigateAsync(e.Uri)` on the current `IWebView` instance

#### Scenario: Handled new window does not trigger fallback
- **WHEN** a handler sets `Handled = true` on the `NewWindowRequested` event
- **THEN** the runtime does not perform any automatic navigation

#### Scenario: Null URI with unhandled event takes no action
- **WHEN** a `NewWindowRequested` event is raised with `Uri == null` and `Handled == false`
- **THEN** the runtime takes no action

### Requirement: Navigation error categorization
When a navigation completes with `Status == Failure`, the `Error` property of `NavigationCompletedEventArgs` SHALL be an instance of a `WebViewNavigationException` subclass that categorizes the failure:
- `WebViewNetworkException` for connectivity-related failures
- `WebViewSslException` for TLS/certificate-related failures
- `WebViewTimeoutException` for request timeout failures
- `WebViewNavigationException` (base) for uncategorized or platform-specific failures

The runtime SHALL preserve the categorized exception from the adapter's `NavigationCompleted` event args when constructing the public `NavigationCompleted` event.

#### Scenario: Network failure is categorized
- **WHEN** the adapter reports a navigation failure caused by a network issue (DNS, connectivity, unreachable host)
- **THEN** `NavigationCompleted.Error` is an instance of `WebViewNetworkException`

#### Scenario: SSL failure is categorized
- **WHEN** the adapter reports a navigation failure caused by a certificate issue
- **THEN** `NavigationCompleted.Error` is an instance of `WebViewSslException`

#### Scenario: Timeout failure is categorized
- **WHEN** the adapter reports a navigation failure caused by a request timeout
- **THEN** `NavigationCompleted.Error` is an instance of `WebViewTimeoutException`

#### Scenario: Uncategorized failure uses base exception
- **WHEN** the adapter reports a navigation failure with a generic or unmapped error
- **THEN** `NavigationCompleted.Error` is an instance of `WebViewNavigationException`

### Requirement: NavigateToStringAsync baseUrl semantics
When `NavigateToStringAsync(html, baseUrl)` is called with a non-null `baseUrl`:
- `Source` SHALL be set to `baseUrl` (not `about:blank`)
- `NavigationStarted` SHALL be raised with `RequestUri == baseUrl`
When `baseUrl` is `null`, behavior SHALL be identical to the existing semantics (`Source == about:blank`).

#### Scenario: Non-null baseUrl sets Source to baseUrl
- **WHEN** `NavigateToStringAsync(html, new Uri("https://example.com/"))` completes
- **THEN** `Source` equals `https://example.com/` and `NavigationStarted.RequestUri` was `https://example.com/`

#### Scenario: Null baseUrl preserves about:blank semantics
- **WHEN** `NavigateToStringAsync(html, null)` completes
- **THEN** `Source` equals `about:blank`

### Requirement: Cookie operation thread safety
Cookie operations obtained via `TryGetCookieManager()` SHALL be callable from any thread.
The runtime SHALL marshal cookie operations to the appropriate context (e.g., main thread for WKHTTPCookieStore) transparently.

#### Scenario: Cookie operation from background thread succeeds
- **WHEN** `GetCookiesAsync` is called from a background thread
- **THEN** the operation completes successfully without throwing a threading exception

### Requirement: Cookie operations on disposed WebView
Cookie operations SHALL throw `ObjectDisposedException` if the owning `IWebView` has been disposed.

#### Scenario: Cookie operation after dispose throws
- **WHEN** `GetCookiesAsync` is called after the `IWebView` is disposed
- **THEN** the operation throws `ObjectDisposedException`

## MODIFIED Requirements

### Requirement: Source and NavigateToString semantics
`Source` SHALL represent the last requested navigation target:
- After `NavigateAsync(uri)` (or setting `Source=uri`), `Source` SHALL equal `uri`.
- After `NavigateToStringAsync(html)`, `Source` SHALL equal `about:blank`.
- After `NavigateToStringAsync(html, baseUrl)` with non-null `baseUrl`, `Source` SHALL equal `baseUrl`.
- After `NavigateToStringAsync(html, null)`, `Source` SHALL equal `about:blank`.
Setting `Source=null` SHALL throw `ArgumentNullException`.
`NavigateToStringAsync(html)` SHALL raise `NavigationStarted` with `RequestUri=about:blank`.
`NavigateToStringAsync(html, baseUrl)` with non-null `baseUrl` SHALL raise `NavigationStarted` with `RequestUri=baseUrl`.

#### Scenario: NavigateToString sets Source to about:blank
- **WHEN** `NavigateToStringAsync("<html>...</html>")` completes successfully
- **THEN** `Source` equals `about:blank` and the started request URI was `about:blank`

#### Scenario: NavigateToString with baseUrl sets Source to baseUrl
- **WHEN** `NavigateToStringAsync("<html>...</html>", new Uri("https://example.com/"))` completes successfully
- **THEN** `Source` equals `https://example.com/` and the started request URI was `https://example.com/`
