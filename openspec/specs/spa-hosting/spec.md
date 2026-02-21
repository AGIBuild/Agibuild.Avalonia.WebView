## Purpose
Define SPA hosting contracts for embedded-resource serving and development proxy behavior.

## Requirements

### Requirement: SpaHostingOptions define deterministic hosting configuration
`SpaHostingOptions` SHALL define scheme/host/fallback configuration, production resource settings, development proxy URL, bridge auto-injection behavior, and default headers.

#### Scenario: Hosting options configure both production and development paths
- **WHEN** an app configures `SpaHostingOptions`
- **THEN** the runtime can resolve deterministic production and development hosting behavior

### Requirement: Embedded-resource serving and SPA fallback are deterministic
SPA hosting SHALL resolve custom-scheme requests from embedded resources and SHALL apply fallback-document behavior for router paths according to configured policy.

#### Scenario: Missing route resolves to fallback document
- **WHEN** a route without a physical asset is requested under SPA hosting
- **THEN** the configured fallback document is served before terminal not-found handling

### Requirement: Development proxy behavior is governed
Development mode SHALL proxy to the configured dev server, SHALL apply configured fallback behavior on non-success responses, and SHALL return deterministic gateway failure diagnostics when the upstream is unreachable.

#### Scenario: Unreachable dev server returns gateway failure
- **WHEN** the configured dev server is unavailable
- **THEN** SPA hosting returns deterministic gateway-failure behavior for the request

### Requirement: MIME and cache policy behavior is explicit
SPA hosting SHALL apply deterministic MIME detection and cache headers for hashed and non-hashed assets.

#### Scenario: Hashed assets receive immutable cache policy
- **WHEN** a hashed static asset is served
- **THEN** response headers include long-lived immutable cache directives

### Requirement: WebViewCore integration lifecycle is deterministic
`EnableSpaHosting(options)` SHALL register scheme/resource interception hooks and SHALL dispose hosting resources during runtime teardown.

#### Scenario: Hosting resources are released on dispose
- **WHEN** runtime teardown is executed
- **THEN** SPA hosting integration resources are disposed without leakage
