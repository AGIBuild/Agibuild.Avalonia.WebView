# Plugin Auth Token — Spec

## Purpose

Define requirements for the Fulora Auth Token bridge plugin. Enables platform-secure token storage from JavaScript through the host, with token metadata (expiry, scope), refresh token support, and integration with the HttpClient plugin for automatic auth header injection.

## Requirements

### Requirement: Plugin implements IBridgePlugin and exposes IAuthTokenService

The Auth Token plugin SHALL implement `IBridgePlugin` and expose `IAuthTokenService` as a bridge service, following the established plugin convention.

#### Scenario: Plugin declares IAuthTokenService via GetServices

- **WHEN** the AuthToken plugin is registered via `Bridge.UsePlugin<AuthTokenPlugin>()`
- **THEN** the plugin SHALL return a service descriptor for `IAuthTokenService`
- **AND** the service SHALL be accessible from JS via the bridge under the registered service name

#### Scenario: Plugin has companion npm package

- **WHEN** the plugin is published
- **THEN** `@agibuild/bridge-plugin-auth-token` SHALL be available on npm
- **AND** the npm package SHALL export TypeScript types for `IAuthTokenService` methods and DTOs

---

### Requirement: IAuthTokenService provides secure token CRUD

The `IAuthTokenService` SHALL expose `getToken`, `setToken`, `removeToken`, and `listKeys` for secure token storage operations.

#### Scenario: setToken stores token in platform-secure storage

- **WHEN** JS calls `authToken.setToken("access_token", "eyJ...")` with a key and value
- **THEN** the host SHALL store the token in platform-secure storage (Keychain, Credential Manager, Keystore, Secret Service)
- **AND** the operation SHALL be async (returns Promise)
- **AND** the token SHALL NOT be stored in plaintext in app-accessible file system

#### Scenario: getToken retrieves stored token

- **WHEN** a token was previously stored with `setToken("access_token", value)`
- **AND** JS calls `authToken.getToken("access_token")`
- **THEN** the host SHALL return the stored token value
- **AND** the operation SHALL be async
- **AND** the returned value SHALL match the originally stored value

#### Scenario: getToken returns null for non-existent key

- **WHEN** JS calls `authToken.getToken("nonexistent")` for a key that was never set
- **THEN** the method SHALL return null (or undefined)
- **AND** SHALL NOT throw

#### Scenario: removeToken deletes stored token

- **WHEN** a token exists for key "old_token"
- **AND** JS calls `authToken.removeToken("old_token")`
- **THEN** the host SHALL remove the token from secure storage
- **AND** subsequent `getToken("old_token")` SHALL return null
- **AND** the operation SHALL be async

#### Scenario: listKeys returns all stored token keys

- **WHEN** tokens have been stored for keys "access_token", "refresh_token", "api_key"
- **AND** JS calls `authToken.listKeys()`
- **THEN** the method SHALL return an array containing those keys (or their namespaced equivalents)
- **AND** the operation SHALL be async
- **AND** when no tokens are stored, SHALL return an empty array

---

### Requirement: Token metadata (expiry, scope) is supported

The `setToken` method SHALL accept optional metadata for expiry and scope. Metadata SHALL be stored and retrievable.

#### Scenario: setToken with options stores metadata

- **WHEN** JS calls `authToken.setToken("access_token", value, { expiresAt: "2025-12-31T23:59:59Z", scope: "read write", tokenType: "access" })`
- **THEN** the host SHALL store the token with the associated metadata
- **AND** the metadata SHALL be retrievable (via getToken return object or getTokenMetadata)

#### Scenario: getToken returns metadata when available

- **WHEN** a token was stored with metadata (expiresAt, scope, tokenType)
- **AND** JS calls `getToken("access_token")` (or `getTokenMetadata`)
- **THEN** the method SHALL return the token value and metadata
- **AND** the caller MAY use expiresAt to determine if refresh is needed

#### Scenario: Metadata is optional

- **WHEN** JS calls `setToken("key", value)` without options
- **THEN** the token SHALL be stored without metadata
- **AND** getToken SHALL return the value (metadata fields may be null/undefined)

---

### Requirement: Platform-secure storage per adapter

The plugin SHALL use platform-native secure storage for each target platform.

#### Scenario: macOS and iOS use Keychain

- **WHEN** the app runs on macOS or iOS
- **THEN** the plugin SHALL use the Keychain API (kSecClassGenericPassword)
- **AND** tokens SHALL be stored with the app's service/identifier
- **AND** tokens SHALL be protected by the Keychain access controls

#### Scenario: Windows uses Credential Manager

- **WHEN** the app runs on Windows
- **THEN** the plugin SHALL use the Windows Credential Manager API
- **AND** tokens SHALL be stored with a target name scoped to the app
- **AND** credentials SHALL be protected by the user's Windows credential store

#### Scenario: Android uses Keystore

- **WHEN** the app runs on Android
- **THEN** the plugin SHALL use Android Keystore (or EncryptedSharedPreferences)
- **AND** tokens SHALL be encrypted at rest
- **AND** storage SHALL be scoped to the app

#### Scenario: Linux uses Secret Service

- **WHEN** the app runs on Linux
- **THEN** the plugin SHALL use libsecret (Secret Service API)
- **AND** tokens SHALL be stored in a collection scoped to the app
- **AND** the plugin SHALL document libsecret as a dependency or requirement

---

### Requirement: IAuthTokenProvider integration for HttpClient plugin

The Auth Token plugin SHALL provide an `IAuthTokenProvider` implementation that the HttpClient plugin can use for auth header injection.

#### Scenario: IAuthTokenProvider returns token for configured key/scope

- **WHEN** the AuthToken plugin is registered and a token is stored for key "default" (or configured scope)
- **AND** the HttpClient plugin is configured to use IAuthTokenProvider
- **AND** the HttpClient plugin is about to send a request
- **THEN** the HttpClient plugin SHALL call the provider to obtain a token
- **AND** the AuthToken plugin's provider implementation SHALL return the stored token for the configured key
- **AND** the HttpClient plugin SHALL add `Authorization: Bearer <token>` to the request headers

#### Scenario: IAuthTokenProvider returns null when token not found or expired

- **WHEN** no token is stored for the configured key, or the token is expired (per metadata)
- **AND** the provider is invoked
- **THEN** the provider SHALL return null
- **AND** the HttpClient plugin SHALL NOT add an Authorization header
- **AND** the request SHALL proceed without auth (or fail per app logic)

---

### Requirement: fulora-plugin.json manifest

The plugin SHALL include a `fulora-plugin.json` manifest for discovery and installation.

#### Scenario: Manifest includes required fields

- **WHEN** the AuthToken plugin package is built
- **THEN** the package SHALL contain `fulora-plugin.json` at the package root
- **AND** the manifest SHALL include: `id`, `displayName`, `services` (including `IAuthTokenService`), `npmPackage` (`@agibuild/bridge-plugin-auth-token`)
