# Official Plugin: Auth Token — Design

## Context

Hybrid apps need secure token storage and management for OAuth access/refresh tokens, API keys, and other sensitive credentials. Storing tokens in JavaScript `localStorage` is insecure — keys are not encrypted, and XSS can exfiltrate them. A bridge plugin stores tokens in platform-secure storage (Keychain, Credential Manager, Keystore, Secret Service) and provides typed bridge access. The HttpClient plugin can consume tokens via an integration point for automatic auth header injection.

**Existing contracts**: `IBridgePlugin`, `UsePlugin<T>`, NuGet+npm dual distribution. Reference plugin: LocalStorage. HttpClient plugin defines `IAuthTokenProvider` for auth header injection.

## Goals / Non-Goals

### Goals

- Platform-secure token storage (CRUD) from JavaScript via the bridge
- Token metadata (expiry, scope) for refresh and rotation logic
- Integration with HttpClient plugin via `IAuthTokenProvider`
- Support for access/refresh token pairs and rotation

### Non-Goals

- OAuth flow management (use WebAuthBroker or equivalent)
- OIDC discovery
- Session management

## Decisions

### D1: Platform secure storage adapters (ISecureStorageProvider)

**Decision**: The plugin SHALL use an `ISecureStorageProvider` abstraction per platform. Each platform adapter implements this interface. The `AuthTokenService` delegates all storage operations to the resolved provider. Providers are registered via DI or platform detection at runtime.

**Rationale**: Platform APIs differ significantly — macOS/iOS Keychain, Windows Credential Manager, Android Keystore, Linux Secret Service (libsecret). A provider abstraction isolates platform-specific code and enables testability via mock providers. Single implementation per platform keeps the plugin maintainable.

### D2: Storage mapping per platform

**Decision**: Each platform SHALL use its native secure storage:
- **macOS/iOS**: Keychain with `kSecClassGenericPassword`, service name = app identifier
- **Windows**: Credential Manager (Credential Manager API) with target name = app identifier
- **Android**: Android Keystore with encrypted SharedPreferences or Keystore-backed storage
- **Linux**: Secret Service (libsecret) with collection = app identifier

**Rationale**: These are the standard, well-tested secure storage mechanisms per platform. Each provides encryption at rest and appropriate access controls. App identifiers scope storage to the app.

### D3: Token metadata (expiry, scope)

**Decision**: The `setToken` method SHALL accept optional `TokenOptions`: `expiresAt` (ISO 8601 timestamp), `scope` (string, e.g., "read write"), `tokenType` (e.g., "access", "refresh"). Metadata SHALL be stored alongside the token value. `getToken` MAY return metadata (or a separate `getTokenMetadata` method). The service SHALL NOT automatically delete expired tokens; callers check expiry and refresh as needed.

**Rationale**: OAuth tokens have expiry; refresh tokens are long-lived. Storing metadata enables JS or host logic to decide when to refresh. Scope helps with multi-scope token management. Explicit expiry check (no auto-delete) keeps the plugin simple and predictable — callers control refresh flow.

### D4: IAuthTokenProvider interface for HttpClient integration

**Decision**: The AuthToken plugin SHALL implement `IAuthTokenProvider` (or provide an adapter). When the HttpClient plugin is configured to use auth token injection, it SHALL resolve `IAuthTokenProvider` from DI. The provider SHALL return a token for a given scope or key (e.g., "default", "api"). The AuthToken plugin's implementation SHALL read from its secure storage and return the token (or null if not found/expired). The host MAY configure which key/scope the HttpClient uses.

**Rationale**: HttpClient plugin already defines `IAuthTokenProvider` for auth header injection. The AuthToken plugin is the natural implementation — it holds tokens in secure storage. Integration via DI keeps plugins loosely coupled. Scope/key allows multiple token sets (e.g., different API backends).

### D5: Key naming and namespacing

**Decision**: Token keys SHALL be strings. The plugin SHALL support a configurable key prefix (e.g., "fulora.auth.") to avoid collisions with other app data. Keys SHALL NOT contain path separators or control characters. `listKeys()` returns keys without the prefix (or with, per config) so callers can enumerate stored tokens.

**Rationale**: Simple string keys are bridge-friendly. Prefix enables multi-tenant or namespaced storage. Restricting key format prevents injection or path traversal in storage backends.

### D6: Refresh token rotation support

**Decision**: The plugin SHALL support storing both access and refresh tokens under related keys (e.g., `access_token`, `refresh_token` or `{scope}.access`, `{scope}.refresh`). When the host or JS refreshes tokens, `setToken` SHALL atomically update both. No built-in refresh logic — the plugin is storage only; refresh flow is implemented by the app (JS or C#).

**Rationale**: OAuth refresh involves calling the token endpoint. That belongs in app logic or a separate auth library. The plugin's job is secure storage; rotation is a usage pattern supported by the CRUD API.

## Risks / Trade-offs

### R1: Platform storage availability

**Risk**: Linux Secret Service may not be available on all distros (e.g., headless servers). Android Keystore has version-specific APIs.

**Mitigation**: Document platform requirements. Provide fallback (e.g., encrypted file in app data) or fail with clear error. Consider optional platform packages.

### R2: Token value exposure in bridge

**Risk**: Token values cross the bridge as strings. The bridge transport (e.g., JSON-RPC) may log or cache messages.

**Mitigation**: Ensure bridge does not log message bodies by default. Tokens are in memory briefly; secure storage protects at rest. Document that tokens are sensitive — avoid logging in app code.

### R3: Keychain/Credential Manager access across app updates

**Risk**: App identifier or key format changes could lose access to stored tokens.

**Mitigation**: Use stable app identifier. Document key naming for upgrades. Consider migration path for key format changes.
