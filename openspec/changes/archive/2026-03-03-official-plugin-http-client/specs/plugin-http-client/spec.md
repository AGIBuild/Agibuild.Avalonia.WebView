# Plugin HTTP Client — Spec

## Purpose

Define requirements for the Fulora HTTP client bridge plugin. Enables host-routed HTTP requests from JavaScript with certificate pinning, auth injection, interceptors, and typed request/response.

## Requirements

### Requirement: Plugin implements IBridgePlugin and exposes IHttpClientService

The HTTP client plugin SHALL implement `IBridgePlugin` and expose `IHttpClientService` as a bridge service, following the established plugin convention.

#### Scenario: Plugin declares IHttpClientService via GetServices

- **WHEN** the HttpClient plugin is registered via `Bridge.UsePlugin<HttpClientPlugin>()`
- **THEN** the plugin SHALL return a service descriptor for `IHttpClientService`
- **AND** the service SHALL be accessible from JS via the bridge under the registered service name

#### Scenario: Plugin has companion npm package

- **WHEN** the plugin is published
- **THEN** `@agibuild/bridge-plugin-http-client` SHALL be available on npm
- **AND** the npm package SHALL export TypeScript types for `IHttpClientService` methods and DTOs

---

### Requirement: IHttpClientService provides typed HTTP methods

The `IHttpClientService` SHALL expose `get`, `post`, `put`, `delete`, and `patch` methods with typed request and response bodies.

#### Scenario: GET request returns JSON response

- **WHEN** JS calls `httpClient.get<T>(url)` with a valid URL
- **THEN** the host SHALL perform an HTTP GET request via `HttpClient`
- **AND** the response body SHALL be deserialized from JSON and returned as typed `T`

#### Scenario: POST request sends JSON body

- **WHEN** JS calls `httpClient.post<TRequest, TResponse>(url, body)` with a body object
- **THEN** the host SHALL perform an HTTP POST request with `Content-Type: application/json`
- **AND** the request body SHALL be serialized from `TRequest` to JSON
- **AND** the response SHALL be deserialized to `TResponse`

#### Scenario: PUT, DELETE, PATCH follow same pattern

- **WHEN** JS calls `httpClient.put`, `httpClient.delete`, or `httpClient.patch` with appropriate parameters
- **THEN** the host SHALL perform the corresponding HTTP method
- **AND** request/response bodies SHALL be JSON-serialized/deserialized consistently

#### Scenario: Relative URLs resolve against base URL

- **WHEN** a base URL is configured (e.g., `https://api.example.com`)
- **AND** JS calls `httpClient.get("/users")` with a relative path
- **THEN** the request SHALL be sent to `https://api.example.com/users`

#### Scenario: Absolute URLs bypass base URL

- **WHEN** JS calls `httpClient.get("https://other.example.com/data")` with an absolute URL
- **THEN** the request SHALL be sent to the specified URL
- **AND** the base URL SHALL NOT be applied

---

### Requirement: Configurable timeout

The plugin SHALL support a configurable timeout for HTTP requests.

#### Scenario: Global timeout is applied

- **WHEN** a timeout is configured (e.g., 30 seconds)
- **AND** an HTTP request exceeds that duration
- **THEN** the request SHALL be cancelled
- **AND** the bridge call SHALL fail with a timeout error

#### Scenario: Per-request timeout overrides global

- **WHEN** a method accepts an optional timeout parameter
- **AND** the caller passes a timeout for a specific request
- **THEN** that timeout SHALL override the global timeout for that request

---

### Requirement: Request interceptor pipeline

The plugin SHALL support a configurable request interceptor pipeline that runs before each request is sent.

#### Scenario: Interceptors modify request

- **WHEN** one or more request interceptors are registered
- **THEN** each interceptor SHALL be invoked in order before the request is sent
- **AND** interceptors MAY modify headers, body, or URL
- **AND** the modified request SHALL be used for the actual HTTP call

#### Scenario: Auth interceptor injects token

- **WHEN** `IAuthTokenProvider` is registered in DI and the plugin is configured to use it
- **THEN** the auth interceptor SHALL call the provider to obtain a token
- **AND** if a token is returned, the interceptor SHALL add `Authorization: Bearer <token>` to the request headers

---

### Requirement: Host controls HttpClient configuration

The host SHALL configure the underlying `HttpClient` for certificate pinning, proxy, and logging.

#### Scenario: Host provides HttpClient via DI

- **WHEN** the host registers an `HttpClient` (or `IHttpClientFactory`) in DI
- **THEN** the plugin SHALL use that instance for all requests
- **AND** the host's configuration (handlers, base address, etc.) SHALL apply

#### Scenario: Plugin creates HttpClient when not provided

- **WHEN** no `HttpClient` is registered in DI
- **THEN** the plugin SHALL create a default `HttpClient` instance
- **AND** the plugin SHALL apply base URL and timeout from config

---

### Requirement: Errors are propagated to JS

HTTP errors and exceptions SHALL be propagated to the JavaScript caller with sufficient context.

#### Scenario: HTTP error status code

- **WHEN** the server returns an HTTP error status (e.g., 404, 500)
- **THEN** the bridge call SHALL fail with an error that includes the status code and error body
- **AND** the error SHALL be catchable from JS

#### Scenario: Network failure

- **WHEN** the request fails due to network error
- **THEN** the bridge call SHALL fail with a network error
- **AND** the error message SHALL indicate the failure reason

---

### Requirement: fulora-plugin.json manifest

The plugin SHALL include a `fulora-plugin.json` manifest for discovery and installation.

#### Scenario: Manifest includes required fields

- **WHEN** the HttpClient plugin package is built
- **THEN** the package SHALL contain `fulora-plugin.json` at the package root
- **AND** the manifest SHALL include: `id`, `displayName`, `services` (including `IHttpClientService`), `npmPackage` (`@agibuild/bridge-plugin-http-client`)
