# Official Plugin: HTTP Client — Design

## Context

SPAs in hybrid apps need to make HTTP requests. Browser `fetch()` works but bypasses host-side concerns: certificate pinning, auth token injection, request logging, and proxy configuration. A bridge-based HTTP client plugin routes requests through C# `HttpClient`, enabling host-controlled networking with full bridge type safety.

**Existing contracts**: `IBridgePlugin`, `UsePlugin<T>`, NuGet+npm dual distribution. Reference plugin: LocalStorage.

## Goals / Non-Goals

### Goals

- Host-routed HTTP requests (GET, POST, PUT, DELETE, PATCH) via bridge
- Certificate pinning, auth header injection, request logging, proxy configuration
- Typed request/response with JSON bodies
- Request/response interceptor pipeline configurable from C#
- Configurable base URL and timeout

### Non-Goals

- WebSocket proxy, gRPC proxy, GraphQL layer
- Replacing browser fetch entirely
- Offline request queuing
- Streaming request/response bodies (deferred to v2)

## Decisions

### D1: Wrap System.Net.Http.HttpClient

**Decision**: The plugin SHALL wrap `System.Net.Http.HttpClient` for all outbound HTTP requests. The host configures the `HttpClient` instance (or uses a factory) with certificate validation, proxy, and other handlers.

**Rationale**: `HttpClient` is the standard .NET abstraction for HTTP. Reusing it ensures compatibility with existing .NET patterns (HttpClientFactory, Polly, etc.) and avoids reinventing networking. Host controls TLS, proxy, and handler pipeline.

### D2: JSON request/response bodies

**Decision**: Request and response bodies SHALL be JSON-serialized/deserialized. The bridge passes JSON strings; C# deserializes to/from DTOs. Non-JSON content types (e.g., `application/octet-stream`) are out of scope for v1.

**Rationale**: Bridge RPC is JSON-based. Typed DTOs on both sides map naturally to JSON. Binary payloads would require base64 encoding and add complexity; defer to a future iteration.

### D3: Streaming not in v1

**Decision**: v1 SHALL NOT support streaming request or response bodies. All requests and responses are fully buffered. Large payloads are supported up to configured limits.

**Rationale**: Streaming adds significant complexity (chunked transfer, backpressure, cancellation). Most SPA API calls are request/response. Streaming can be added in v2 if needed.

### D4: Request interceptor pipeline

**Decision**: The plugin SHALL support a configurable request interceptor pipeline. Interceptors run in order before the request is sent; they MAY modify headers, body, or URL. Response interceptors (post-receive) MAY be added for logging or transformation.

**Rationale**: Interceptors enable auth header injection, logging, retry logic, and custom headers without coupling the service to specific auth logic. Host registers interceptors via DI or options.

### D5: Configurable base URL

**Decision**: The `IHttpClientService` SHALL support a configurable base URL. All relative URLs are resolved against this base. Absolute URLs bypass the base URL.

**Rationale**: SPAs often call a single API origin. Base URL reduces repetition and enables environment-specific configuration (dev/staging/prod) from C#.

### D6: Configurable timeout

**Decision**: The plugin SHALL support a configurable per-request and/or global timeout. Default timeout SHALL be documented (e.g., 30 seconds). Timeout applies to the entire request-response cycle.

**Rationale**: Long-running requests can block the bridge. Configurable timeout prevents indefinite hangs and allows tuning per use case.

### D7: IAuthTokenProvider integration

**Decision**: When `IAuthTokenProvider` is registered in DI, the plugin SHALL optionally inject an auth header (e.g., `Authorization: Bearer <token>`) into requests. The provider interface returns a token (or null); the plugin adds it to the request if present.

**Rationale**: Auth tokens are a common host concern. Centralizing injection in C# keeps tokens out of JS and enables token refresh logic on the host.

## Risks / Trade-offs

### R1: JSON-only limits binary use cases

**Risk**: APIs that require multipart/form-data, file uploads, or binary responses are not fully supported in v1.

**Mitigation**: Document v1 scope. Add `Content-Type` and body format support in v2 if needed.

### R2: Interceptor ordering and side effects

**Risk**: Interceptors can conflict (e.g., multiple auth providers) or have unclear ordering.

**Mitigation**: Document interceptor registration order. Provide a simple default: auth interceptor first, then logging. Allow host to control order via options.

### R3: Timeout vs bridge call timeout

**Risk**: Bridge calls have their own timeout. HTTP timeout and bridge timeout can interact in confusing ways.

**Mitigation**: HTTP timeout SHALL be less than or equal to bridge call timeout where applicable. Document the relationship.
