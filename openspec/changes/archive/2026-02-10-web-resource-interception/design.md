# Design: Web Resource Interception

## Architecture

```
Consumer
  │  RegisterCustomScheme("app")  (before attach)
  │  WebResourceRequested += handler
  │
  ▼
WebView / WebViewCore
  │  Stores scheme list in IWebViewEnvironmentOptions.CustomSchemes
  │  Raises WebResourceRequestedEventArgs on UI thread
  │
  ▼
IWebViewAdapter (+ new ICustomSchemeAdapter facet)
  │  RegisterCustomScheme() called during Initialize/Attach
  │  Intercepts requests → raises WebResourceRequested
  │
  ▼
Platform-native API
  Windows: CoreWebView2CustomSchemeRegistration + WebResourceRequested filter
  macOS/iOS: WKURLSchemeHandler (registered on WKWebViewConfiguration)
  GTK: webkit_web_context_register_uri_scheme()
  Android: WebViewClient.shouldInterceptRequest()
```

## Key Decisions

### D1: Custom scheme adapter as optional facet
Similar to `ICookieAdapter`, create `ICustomSchemeAdapter` interface that adapters MAY implement. Runtime detects support via type check. This avoids breaking existing adapter contract.

### D2: Scheme registration timing
Schemes MUST be registered before `Attach()` because:
- WKWebView requires schemes in `WKWebViewConfiguration` (immutable after creation)
- WebView2 requires `CoreWebView2CustomSchemeRegistration` before environment creation
- Design: `IWebViewEnvironmentOptions.CustomSchemes` carries the list; adapters read it during init

### D3: Response body as Stream (not just string)
Change `ResponseBody` from `string?` to `Stream?` to support binary content (images, fonts, WASM).
Keep `ResponseContentType` and `ResponseStatusCode`. Add `ResponseHeaders` dictionary.

### D4: Standard HTTP/HTTPS interception
Only Windows (`AddWebResourceRequestedFilter`) and Android (`shouldInterceptRequest`) support intercepting standard schemes. macOS/iOS/GTK can only intercept custom schemes.
The `ICustomSchemeAdapter` contract does NOT require standard scheme interception. Document platform capabilities.

### D5: Thread model
`WebResourceRequested` is raised on the **UI thread** via `IWebViewDispatcher`.
Response must be set synchronously in the handler (no async handler support in v1).
Adapters that receive requests on background threads marshal to UI thread before raising.

## Event Args Changes

```csharp
[Experimental("AGWV004")]
public sealed class WebResourceRequestedEventArgs : EventArgs
{
    public Uri? RequestUri { get; init; }
    public string Method { get; init; } = "GET";
    public IReadOnlyDictionary<string, string>? RequestHeaders { get; init; }

    // Response (set by handler)
    public bool Handled { get; set; }
    public Stream? ResponseBody { get; set; }
    public string ResponseContentType { get; set; } = "text/html";
    public int ResponseStatusCode { get; set; } = 200;
    public IDictionary<string, string>? ResponseHeaders { get; set; }
}
```

## New Types

```csharp
public sealed class CustomSchemeRegistration
{
    public string SchemeName { get; init; }
    public bool HasAuthorityComponent { get; init; }
    public bool TreatAsSecure { get; init; }
}
```

Added to `IWebViewEnvironmentOptions`:
```csharp
IReadOnlyList<CustomSchemeRegistration> CustomSchemes { get; }
```
