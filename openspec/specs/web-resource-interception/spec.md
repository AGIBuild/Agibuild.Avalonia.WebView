## Purpose
Define web-resource interception contracts for custom schemes and request/response handling.

## Requirements

### Requirement: CustomSchemeRegistration type in Core
The Core assembly SHALL define `CustomSchemeRegistration` with:
- `string SchemeName { get; init; }` — the custom scheme name (e.g., "app")
- `bool HasAuthorityComponent { get; init; }` — whether URIs include host/authority
- `bool TreatAsSecure { get; init; }` — whether to treat as secure context

#### Scenario: CustomSchemeRegistration is constructible
- **WHEN** a consumer creates `new CustomSchemeRegistration { SchemeName = "app" }`
- **THEN** it compiles and the properties are set correctly

### Requirement: IWebViewEnvironmentOptions includes CustomSchemes
The `IWebViewEnvironmentOptions` interface SHALL define:
- `IReadOnlyList<CustomSchemeRegistration> CustomSchemes { get; }`

`WebViewEnvironmentOptions` SHALL default `CustomSchemes` to an empty list.

#### Scenario: Custom schemes are configurable before WebView creation
- **WHEN** a consumer sets `CustomSchemes` on `WebViewEnvironmentOptions` before `WebViewEnvironment.Initialize()`
- **THEN** the schemes are available to adapters during initialization

### Requirement: ICustomSchemeAdapter facet for adapters
The adapter abstractions SHALL define `ICustomSchemeAdapter` that adapters MAY implement alongside `IWebViewAdapter`:
- `void RegisterCustomSchemes(IReadOnlyList<CustomSchemeRegistration> schemes)`

The runtime SHALL detect `ICustomSchemeAdapter` support via type check and call `RegisterCustomSchemes()` before `Attach()`.

#### Scenario: Adapter implementing ICustomSchemeAdapter receives schemes
- **WHEN** an adapter implements `ICustomSchemeAdapter` and custom schemes are configured
- **THEN** `RegisterCustomSchemes()` is called before `Attach()` with the configured schemes

#### Scenario: Adapter without ICustomSchemeAdapter skips scheme registration
- **WHEN** an adapter does NOT implement `ICustomSchemeAdapter`
- **THEN** no scheme registration is attempted and no error occurs

### Requirement: WebResourceRequestedEventArgs supports binary responses
The `WebResourceRequestedEventArgs` SHALL define:
- `Uri? RequestUri { get; }` — the intercepted request URI
- `string Method { get; }` — HTTP method, default "GET"
- `IReadOnlyDictionary<string, string>? RequestHeaders { get; }` — request headers
- `bool Handled { get; set; }` — set to true when handler provides a response
- `Stream? ResponseBody { get; set; }` — response content stream
- `string ResponseContentType { get; set; }` — MIME type, default "text/html"
- `int ResponseStatusCode { get; set; }` — HTTP status, default 200
- `IDictionary<string, string>? ResponseHeaders { get; set; }` — response headers

#### Scenario: Handler provides binary response
- **WHEN** a WebResourceRequested handler sets `Handled = true` with a `MemoryStream` body
- **THEN** the adapter serves the stream content to the WebView

#### Scenario: Unhandled request passes through
- **WHEN** no handler sets `Handled = true`
- **THEN** the request proceeds to the network normally

### Requirement: Adapters raise WebResourceRequested for custom scheme requests
When a custom scheme is registered and the WebView requests a resource with that scheme, the adapter SHALL raise `WebResourceRequested` on the Core event. The event SHALL be raised on the UI thread.

#### Scenario: Custom scheme request triggers event (Windows)
- **WHEN** a page loads `app://localhost/index.html` and "app" is a registered custom scheme
- **THEN** `WebResourceRequested` is raised with `RequestUri` matching the request

#### Scenario: Custom scheme request triggers event (macOS/iOS)
- **WHEN** a page loads `app://localhost/index.html` on macOS/iOS
- **THEN** `WebResourceRequested` is raised via `WKURLSchemeHandler`

#### Scenario: Custom scheme request triggers event (GTK)
- **WHEN** a page loads `app://localhost/index.html` on GTK/Linux
- **THEN** `WebResourceRequested` is raised via `webkit_web_context_register_uri_scheme`

#### Scenario: Custom scheme request triggers event (Android)
- **WHEN** a page loads `app://localhost/index.html` on Android
- **THEN** `WebResourceRequested` is raised via `shouldInterceptRequest`

### Requirement: WebViewCore propagates WebResourceRequested to consumers
`WebViewCore` SHALL forward `WebResourceRequested` events from the adapter to its own event subscribers on the UI thread. The `WebView` control SHALL bubble the event.

#### Scenario: Consumer receives WebResourceRequested on WebView control
- **WHEN** a custom scheme request occurs
- **THEN** the `WebView.WebResourceRequested` event fires with the correct args
