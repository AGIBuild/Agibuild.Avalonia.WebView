## Purpose
Define cookie-management contracts for runtime APIs and adapter-dependent availability.

## Requirements

### Requirement: ICookieManager provides async CRUD operations
The `ICookieManager` interface SHALL define:
- `Task<IReadOnlyList<WebViewCookie>> GetCookiesAsync(Uri uri)` — retrieves cookies matching the given URI
- `Task SetCookieAsync(WebViewCookie cookie)` — sets or updates a cookie
- `Task DeleteCookieAsync(WebViewCookie cookie)` — deletes a cookie matching name, domain, and path
- `Task ClearAllCookiesAsync()` — removes all cookies from the cookie store

All operations SHALL be asynchronous to support platform cookie stores that require completion-handler patterns.

#### Scenario: Get cookies returns matching cookies
- **WHEN** `GetCookiesAsync(uri)` is called with a valid URI
- **THEN** the returned list contains all cookies whose domain and path match the URI

#### Scenario: Set cookie persists a new cookie
- **WHEN** `SetCookieAsync(cookie)` is called with a valid `WebViewCookie`
- **THEN** a subsequent `GetCookiesAsync` for the matching URI includes the cookie

#### Scenario: Delete cookie removes a specific cookie
- **WHEN** `DeleteCookieAsync(cookie)` is called with a cookie matching name, domain, and path
- **THEN** a subsequent `GetCookiesAsync` for the matching URI no longer includes the cookie

#### Scenario: Clear all removes every cookie
- **WHEN** `ClearAllCookiesAsync()` is called
- **THEN** `GetCookiesAsync` returns an empty list for any URI

### Requirement: WebViewCookie value type
The system SHALL define a `WebViewCookie` sealed record with the following properties:
- `string Name` — cookie name (required, non-null)
- `string Value` — cookie value (required, non-null)
- `string Domain` — cookie domain (required, non-null)
- `string Path` — cookie path (required, non-null)
- `DateTimeOffset? Expires` — expiry timestamp (null for session cookies)
- `bool IsSecure` — whether the cookie requires HTTPS
- `bool IsHttpOnly` — whether the cookie is HTTP-only

`WebViewCookie` SHALL be immutable and platform-agnostic.

#### Scenario: WebViewCookie is immutable
- **WHEN** a `WebViewCookie` instance is created
- **THEN** its properties cannot be mutated after construction

### Requirement: ICookieManager availability depends on adapter support
`IWebView.TryGetCookieManager()` SHALL return a non-null `ICookieManager` when the underlying adapter supports cookie management.
`IWebView.TryGetCookieManager()` SHALL return `null` when the underlying adapter does not support cookie management.

#### Scenario: Cookie manager is available when adapter supports it
- **WHEN** the adapter implements cookie support
- **THEN** `TryGetCookieManager()` returns a non-null `ICookieManager`

#### Scenario: Cookie manager is null when adapter lacks support
- **WHEN** the adapter does not implement cookie support
- **THEN** `TryGetCookieManager()` returns `null`

### Requirement: Cookie operations require attached lifecycle
Cookie operations SHALL throw `InvalidOperationException` if invoked before the adapter is attached or after it is detached.

#### Scenario: Cookie operation before attach throws
- **WHEN** `GetCookiesAsync` is called before the adapter is attached
- **THEN** the operation throws `InvalidOperationException`

### Requirement: ICookieManager remains experimental
The `ICookieManager` interface SHALL retain the `[Experimental("AGWV001")]` attribute until validated on at least two platform adapters.

#### Scenario: Experimental attribute is present
- **WHEN** a consumer references `ICookieManager` without suppressing AGWV001
- **THEN** the compiler emits a diagnostic warning
