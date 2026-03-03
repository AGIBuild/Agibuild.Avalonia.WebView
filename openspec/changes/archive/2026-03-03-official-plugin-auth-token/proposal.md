## Why

Hybrid apps need secure token storage and management (OAuth access/refresh tokens, API keys). Storing tokens in JS localStorage is insecure. A bridge plugin stores tokens in platform-secure storage (Keychain/Credential Manager/Keystore) and provides typed bridge access. Goal: Phase 11 M11.3.

## What Changes

- New NuGet: Agibuild.Fulora.Plugin.AuthToken implementing IBridgePlugin
- New npm: @agibuild/bridge-plugin-auth-token with TypeScript types
- [JsExport] IAuthTokenService: getToken(key), setToken(key, value, options), removeToken(key), listKeys()
- Platform-secure storage: macOS/iOS Keychain, Windows Credential Manager, Android Keystore, Linux Secret Service
- Token expiry tracking and refresh token rotation support
- Integration point for HttpClient plugin (IAuthTokenProvider)

## Capabilities

### New Capabilities
- `plugin-auth-token`: Bridge plugin for platform-secure token storage

### Modified Capabilities
(none)

## Non-goals

- OAuth flow management (use WebAuthBroker), OIDC discovery, session management

## Impact

- New project: src/Agibuild.Fulora.Plugin.AuthToken/
- New npm: packages/bridge-plugin-auth-token/
- Platform-specific secure storage per adapter
