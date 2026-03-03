# Official Plugin: HTTP Client — Tasks

## 1. Project Setup

- [x] 1.1 Create `src/Agibuild.Fulora.Plugin.HttpClient/` project with .csproj targeting net8.0
- [x] 1.2 Add package references: `Agibuild.Fulora` (or Bridge core), `System.Net.Http`, `System.Text.Json`
- [x] 1.3 Add `fulora-plugin` and `fulora-plugin-http-client` to `PackageTags`
- [x] 1.4 Create `fulora-plugin.json` manifest with id, displayName, services, npmPackage
- [x] 1.5 Configure manifest as content file packed at package root

## 2. IHttpClientService Contract

- [x] 2.1 Define `IHttpClientService` interface with [JsExport] and methods: `GetAsync`, `PostAsync`, `PutAsync`, `DeleteAsync`, `PatchAsync`
- [x] 2.2 Define request/response DTOs for typed bodies (generic or explicit)
- [x] 2.3 Define `HttpClientPluginOptions` for base URL, timeout, auth provider integration
- [x] 2.4 Define `IHttpRequestInterceptor` and `IHttpResponseInterceptor` interfaces (or equivalent pipeline abstraction)
- [x] 2.5 Define `IAuthTokenProvider` interface (or document integration with existing auth abstraction)

## 3. Implementation

- [x] 3.1 Implement `HttpClientPlugin : IBridgePlugin` with `GetServices()` returning `IHttpClientService` descriptor
- [x] 3.2 Implement `HttpClientService : IHttpClientService` wrapping `HttpClient`
- [x] 3.3 Implement JSON serialization/deserialization for request and response bodies
- [x] 3.4 Implement base URL resolution for relative URLs
- [x] 3.5 Implement configurable timeout (global and per-request)
- [x] 3.6 Resolve `HttpClient` from DI when available; otherwise create default instance

## 4. Interceptor Pipeline

- [x] 4.1 Implement request interceptor pipeline: invoke interceptors in order before sending request
- [x] 4.2 Support interceptor registration via `HttpClientPluginOptions` or DI
- [x] 4.3 Implement auth interceptor that calls `IAuthTokenProvider` and injects `Authorization` header
- [x] 4.4 (Optional) Implement response interceptor for logging or transformation

## 5. npm Package

- [x] 5.1 Create `packages/bridge-plugin-http-client/` with package.json
- [x] 5.2 Generate or hand-write TypeScript types for `IHttpClientService` methods
- [x] 5.3 Export `getHttpClientService()` helper that resolves service from bridge client
- [x] 5.4 Publish npm package as `@agibuild/bridge-plugin-http-client`

## 6. Tests

- [x] 6.1 Unit tests: `HttpClientService` — mock `HttpClient`/`HttpMessageHandler`, verify GET/POST/PUT/DELETE/PATCH
- [x] 6.2 Unit tests: Base URL resolution — relative vs absolute URLs
- [x] 6.3 Unit tests: Timeout — verify request is cancelled after timeout
- [x] 6.4 Unit tests: Interceptor pipeline — verify interceptors run in order and can modify request
- [x] 6.5 Unit tests: Auth interceptor — verify token injection when provider returns token
- [x] 6.6 Integration test: Full request flow from JS through bridge to real HTTP endpoint (or mock server)
