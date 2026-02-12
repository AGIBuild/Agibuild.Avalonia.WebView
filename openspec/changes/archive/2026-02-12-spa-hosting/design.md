# spa-hosting — Design

**ROADMAP**: Phase 2, Deliverables 2.1 + 2.2 + 2.3 + 2.4

## Architecture Layers

### SpaHostingOptions (Core)

- `Scheme`, `Host`, `FallbackDocument`, `EmbeddedResourcePrefix`, `ResourceAssembly`, `DevServerUrl`, `AutoInjectBridgeScript`, `DefaultHeaders`

### SpaHostingService (Runtime)

Handles `WebResourceRequested`. Two modes:

1. **Embedded**: `Assembly.GetManifestResourceStream` for resources under `EmbeddedResourcePrefix`
2. **Dev proxy**: `HttpClient.Send` (synchronous) for requests proxied to `DevServerUrl`

- MIME detection: 40+ types
- Hashed filename → immutable cache
- SPA fallback: no extension or missing file → serve `FallbackDocument`

### SpaHostingExtensions

- `AddEmbeddedFileProvider` / `AddDevServerProxy` convenience methods

### WebViewCore.EnableSpaHosting

- Registers custom scheme
- Subscribes to `WebResourceRequested`
- Auto-enables bridge
- Disposes subscription on cleanup

## Testing

28 Contract Tests covering: options, MIME, caching, fallback, embedded serving, 404, integration with WebViewCore. Includes `EmbeddedResource` test fixture.
