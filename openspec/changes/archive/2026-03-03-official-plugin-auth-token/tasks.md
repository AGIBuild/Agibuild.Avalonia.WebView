# Official Plugin: Auth Token — Tasks

## 1. Project Setup

- [x] 1.1 Create `src/Agibuild.Fulora.Plugin.AuthToken/` project with .csproj targeting net8.0
- [x] 1.2 Add package references: `Agibuild.Fulora` (or Bridge core)
- [x] 1.3 Add `fulora-plugin` and `fulora-plugin-auth-token` to `PackageTags`
- [x] 1.4 Create `fulora-plugin.json` manifest with id, displayName, services, npmPackage
- [x] 1.5 Configure manifest as content file packed at package root
- [x] 1.6 Add platform-specific project references or conditional compilation for Windows, macOS, Linux, Android, iOS

## 2. IAuthTokenService Contract

- [x] 2.1 Define `IAuthTokenService` interface with [JsExport] and methods: `GetTokenAsync(key)`, `SetTokenAsync(key, value, options?)`, `RemoveTokenAsync(key)`, `ListKeysAsync()`
- [x] 2.2 Define `TokenOptions` DTO: expiresAt, scope, tokenType
- [x] 2.3 Define `TokenResult` or `TokenWithMetadata` DTO for getToken return (value, expiresAt, scope, tokenType)
- [x] 2.4 Define `AuthTokenPluginOptions` for key prefix, default scope for HttpClient
- [x] 2.5 Define `IAuthTokenProvider` interface (or reference from HttpClient plugin): `Task<string?> GetTokenAsync(string? scope = null)`

## 3. Platform Adapters (ISecureStorageProvider)

- [x] 3.1 Define `ISecureStorageProvider` interface: GetAsync, SetAsync, RemoveAsync, ListKeysAsync
- [x] 3.2 Implement macOS adapter using Keychain (Security framework, kSecClassGenericPassword)
- [x] 3.3 Implement iOS adapter using Keychain (same as macOS)
- [x] 3.4 Implement Windows adapter using Credential Manager (CredRead/CredWrite)
- [x] 3.5 Implement Android adapter using Keystore or EncryptedSharedPreferences
- [x] 3.6 Implement Linux adapter using libsecret (Secret Service)
- [x] 3.7 Implement provider resolution (platform detection, DI registration)

## 4. Token Metadata

- [x] 4.1 Implement metadata storage alongside token value (serialized JSON or separate keys per metadata field)
- [x] 4.2 Implement SetTokenAsync with TokenOptions: store expiresAt, scope, tokenType
- [x] 4.3 Implement GetTokenAsync to return TokenResult with value and metadata
- [x] 4.4 Document that plugin does NOT auto-delete expired tokens; callers check expiry
- [x] 4.5 Support key prefix in options for namespacing (e.g., "fulora.auth.")

## 5. AuthTokenService Implementation

- [x] 5.1 Implement `AuthTokenService : IAuthTokenService` delegating to ISecureStorageProvider
- [x] 5.2 Implement `AuthTokenPlugin : IBridgePlugin` with GetServices()
- [x] 5.3 Wire plugin to accept AuthTokenPluginOptions (key prefix, default scope)
- [x] 5.4 Apply key prefix to all storage operations when configured

## 6. IAuthTokenProvider Integration (HttpClient Plugin)

- [x] 6.1 Implement `AuthTokenProvider : IAuthTokenProvider` that reads from AuthTokenService/ISecureStorageProvider
- [x] 6.2 Register IAuthTokenProvider in DI when AuthToken plugin is used (or via plugin options)
- [x] 6.3 Resolve scope/key from HttpClient plugin config (e.g., "default", "api")
- [x] 6.4 Return null when token not found or expired (check expiresAt if metadata present)
- [x] 6.5 Document HttpClient plugin integration in plugin setup guide

## 7. npm Package

- [x] 7.1 Create `packages/bridge-plugin-auth-token/` with package.json
- [x] 7.2 Generate or hand-write TypeScript types for IAuthTokenService methods, TokenOptions, TokenResult
- [x] 7.3 Export `getAuthTokenService()` helper that resolves service from bridge client
- [x] 7.4 Publish npm package as `@agibuild/bridge-plugin-auth-token`

## 8. Tests

- [x] 8.1 Unit tests: AuthTokenService with mock ISecureStorageProvider — verify GetTokenAsync, SetTokenAsync, RemoveTokenAsync, ListKeysAsync
- [x] 8.2 Unit tests: Token metadata — verify metadata is stored and retrieved correctly
- [x] 8.3 Unit tests: Key prefix — verify keys are prefixed when option is set
- [x] 8.4 Unit tests: IAuthTokenProvider — verify returns token for configured key, null when not found/expired
- [x] 8.5 Integration test: Full flow from JS setToken through bridge to mock provider, getToken round-trip
- [x] 8.6 Platform-specific tests (or documented manual verification) for each secure storage adapter
- [x] 8.7 Security: Verify tokens are not written to plaintext files in temp or app data
