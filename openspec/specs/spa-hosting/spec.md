# SPA Hosting Spec

## Overview
First-class SPA hosting via custom URL schemes with embedded resources or dev server proxy.

## Requirements

### SH-1: SpaHostingOptions
- Scheme (default: "app"), Host (default: "localhost"), FallbackDocument (default: "index.html")
- EmbeddedResourcePrefix + ResourceAssembly for production mode
- DevServerUrl for development proxy mode
- AutoInjectBridgeScript (default: true)
- DefaultHeaders for CSP/CORS

### SH-2: Embedded resource serving
- Resolves `app://localhost/{path}` to Assembly.GetManifestResourceStream
- Resource name: `{AssemblyName}.{Prefix}.{path.Replace('/','.') }`
- Returns 404 with "Not Found" body if resource missing

### SH-3: SPA router fallback
- Paths without file extension serve FallbackDocument
- Missing files also fallback to FallbackDocument before returning 404

### SH-4: Dev server proxy
- Synchronous HttpClient.Send to dev server URL
- Fallback to FallbackDocument on non-success responses
- Returns 502 on connection failures

### SH-5: MIME type detection
- 40+ extension→MIME mappings for common web assets
- Default: application/octet-stream

### SH-6: Caching headers
- Hashed filenames (8+ hex chars): `Cache-Control: public, max-age=31536000, immutable`
- Other files: `Cache-Control: no-cache`

### SH-7: WebViewCore integration
- `EnableSpaHosting(options)`: registers custom scheme, subscribes WebResourceRequested
- Auto-enables WebMessage bridge if AutoInjectBridgeScript
- Dispose cleans up SpaHostingService

## Test Coverage
- 28 CTs in `SpaHostingTests`
