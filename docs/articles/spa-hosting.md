# SPA Hosting

SPA hosting is the web-first delivery layer for this project’s Electron-replacement path.
The same `app://` URL works in development and production, keeping host glue minimal.

## Architecture Role

SPA hosting complements typed bridge and capability governance:

- serves web assets deterministically via custom scheme
- preserves a stable URL surface across environments
- enables bridge injection and diagnostics-friendly runtime behavior

## Production Mode (Embedded Assets)

### 1) Embed web output

```xml
<ItemGroup>
  <EmbeddedResource Include="wwwroot\**\*" LinkBase="wwwroot" />
</ItemGroup>
```

### 2) Enable hosting

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly,
});

await webView.NavigateAsync(new Uri("app://localhost/index.html"));
```

### 3) Runtime behavior

- `app://localhost/styles.css` resolves to embedded resource
- MIME type is inferred from extension
- hashed assets (for example `main.a1b2c3d4.js`) get immutable caching
- non-hashed assets default to no-cache semantics

## Development Mode (HMR Proxy)

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173",
});
```

- `app://localhost/*` requests are proxied to dev server
- HMR works without changing app URLs
- no environment-specific URL branching in app code

## Router Fallback Semantics

Client-side routing works with automatic fallback:

- `app://localhost/dashboard` -> no extension -> serves `index.html`
- `app://localhost/assets/logo.svg` -> extension present -> serves asset directly

## Bridge Auto-Injection

With default `AutoInjectBridgeScript = true`, `window.agWebView.rpc` is injected for `app://` pages.
This keeps bridge bootstrap consistent across dev and production.

## Configuration Reference

| Property | Default | Description |
|---|---|---|
| `Scheme` | `"app"` | Custom URI scheme |
| `Host` | `"localhost"` | Host for custom scheme |
| `FallbackDocument` | `"index.html"` | Fallback page for extension-less paths |
| `EmbeddedResourcePrefix` | — | Resource root (for example `"wwwroot"`) |
| `ResourceAssembly` | — | Assembly containing embedded resources |
| `DevServerUrl` | — | Proxy target for development mode |
| `AutoInjectBridgeScript` | `true` | Auto-inject bridge client bootstrap |
| `DefaultHeaders` | `{}` | Additional response headers (for example CSP) |

## Environment Extension Methods

```csharp
var options = new WebViewEnvironmentOptions();
options.AddEmbeddedFileProvider("app", typeof(App).Assembly, "wwwroot");
options.AddDevServerProxy("app", "http://localhost:5173");
```

## Governance Recommendations

- keep `app://` as the single navigation surface in all environments
- set explicit security headers through `DefaultHeaders`
- pair SPA hosting with bridge/capability policy for deterministic behavior
- keep diagnostics enabled in CI lanes for route/asset failure visibility

## Related Documents

- [Getting Started](./getting-started.md)
- [Bridge Guide](./bridge-guide.md)
- [Architecture](./architecture.md)
- [Roadmap](../../openspec/ROADMAP.md)
