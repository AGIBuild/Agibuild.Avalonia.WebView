# SPA Hosting

## Overview

Agibuild.Avalonia.WebView provides first-class SPA hosting via custom URL schemes (`app://`). Your web frontend is served from embedded resources (production) or proxied from a dev server (development), with automatic SPA router fallback.

## Production Mode (Embedded Resources)

### 1. Embed web assets

Add to your `.csproj`:
```xml
<ItemGroup>
  <EmbeddedResource Include="wwwroot\**\*" LinkBase="wwwroot" />
</ItemGroup>
```

### 2. Enable SPA hosting

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    EmbeddedResourcePrefix = "wwwroot",
    ResourceAssembly = typeof(MainWindow).Assembly,
});

await webView.NavigateAsync(new Uri("app://localhost/index.html"));
```

### How it works

- `app://localhost/styles.css` → looks for embedded resource `{Assembly}.wwwroot.styles.css`
- 40+ MIME types detected from file extension
- Hashed filenames (e.g., `main.a1b2c3d4.js`) get immutable cache headers
- Non-hashed files get `no-cache`

## Development Mode (Dev Server Proxy)

```csharp
webView.EnableSpaHosting(new SpaHostingOptions
{
    DevServerUrl = "http://localhost:5173", // Vite, webpack, etc.
});
```

- All `app://localhost/*` requests are proxied to the dev server
- Hot Module Replacement (HMR) works transparently
- Your code uses the same `app://` URLs in both modes

## SPA Router Fallback

Client-side routing (React Router, Vue Router) is supported automatically:

- `app://localhost/dashboard` → no file extension → serves `index.html`
- `app://localhost/api/data.json` → has extension → serves the file

## Bridge Auto-Injection

By default (`AutoInjectBridgeScript = true`), the bridge client script is automatically injected into `app://` pages. This means `window.agWebView.rpc` is available without manual script tags.

## Configuration Reference

| Property | Default | Description |
|----------|---------|-------------|
| `Scheme` | `"app"` | Custom URI scheme |
| `Host` | `"localhost"` | Host for the custom scheme |
| `FallbackDocument` | `"index.html"` | Served for paths without extensions |
| `EmbeddedResourcePrefix` | — | Resource prefix (e.g., `"wwwroot"`) |
| `ResourceAssembly` | — | Assembly containing embedded resources |
| `DevServerUrl` | — | Dev server URL for proxy mode |
| `AutoInjectBridgeScript` | `true` | Auto-inject bridge client script |
| `DefaultHeaders` | `{}` | Extra response headers (e.g., CSP) |

## Extension Methods

```csharp
// Shorthand for common configurations
var options = new WebViewEnvironmentOptions();
options.AddEmbeddedFileProvider("app", typeof(App).Assembly, "wwwroot");
options.AddDevServerProxy("app", "http://localhost:5173");
```
