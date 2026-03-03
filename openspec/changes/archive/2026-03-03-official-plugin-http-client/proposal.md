## Why

SPAs in hybrid apps need to make HTTP requests. Browser fetch() works but bypasses host-side concerns: certificate pinning, auth token injection, request logging, and proxy configuration. A bridge-based HTTP client plugin routes requests through C# HttpClient, enabling host-controlled networking with full bridge type safety. Goal: Phase 11 M11.3.

## What Changes

- New NuGet: Agibuild.Fulora.Plugin.HttpClient implementing IBridgePlugin
- New npm: @agibuild/bridge-plugin-http-client with TypeScript types
- [JsExport] IHttpClientService: get/post/put/delete/patch with typed request/response
- Request/response interceptors configurable from C#
- Automatic auth header injection via IAuthTokenProvider integration

## Capabilities

### New Capabilities
- `plugin-http-client`: Bridge plugin for host-routed HTTP requests with interceptors

### Modified Capabilities
(none)

## Non-goals

- WebSocket proxy, gRPC proxy, GraphQL layer
- Replacing browser fetch entirely
- Offline request queuing (separate concern)

## Impact

- New project: src/Agibuild.Fulora.Plugin.HttpClient/
- New npm: packages/bridge-plugin-http-client/
- Dependencies: System.Net.Http (built-in)
