## Purpose

Define Service Worker integration for offline caching in Agibuild.Fulora SPA-hosted apps, with configurable cache strategies and automatic registration.

## ADDED Requirements

### Requirement: Service Worker registration
SPA hosting SHALL support automatic Service Worker registration when loading SPA content in production mode.

#### Scenario: SW registered on SPA load
- **WHEN** the WebView navigates to the SPA root (e.g., `app://localhost/` or `app://localhost/index.html`) and Service Worker is enabled in configuration
- **THEN** the Service Worker SHALL be registered via `navigator.serviceWorker.register(scriptUrl, { scope })`
- **AND** registration SHALL occur from within the loaded page context (injected or page script)

#### Scenario: SW registration is configurable
- **WHEN** the host configures `ServiceWorkerOptions` (or equivalent)
- **THEN** the host SHALL be able to specify the Service Worker script path (or use built-in default), scope, and enable/disable

### Requirement: Cache strategy configuration
Configuration API SHALL support cache strategies: cache-first, network-first, stale-while-revalidate.

#### Scenario: Cache strategy is configurable
- **WHEN** the host configures `ServiceWorkerOptions.CacheStrategy` (or equivalent)
- **THEN** the built-in Service Worker (if used) SHALL apply the specified strategy for fetch interception
- **AND** supported strategies SHALL include at least: CacheFirst, NetworkFirst, StaleWhileRevalidate

#### Scenario: Per-URL pattern overrides (optional)
- **WHEN** the host configures per-URL-pattern overrides (e.g., `/api/*` → network-first)
- **THEN** the Service Worker SHALL apply the override for matching requests
- **AND** this MAY be an optional advanced configuration

### Requirement: Custom Service Worker support
The host SHALL be able to provide a custom Service Worker script instead of the built-in default.

#### Scenario: Custom SW script path
- **WHEN** the host sets `ServiceWorkerOptions.ScriptPath` to a custom script URL or path
- **THEN** the registration SHALL use that script instead of the built-in default
- **AND** the host is responsible for the custom script's behavior

### Requirement: Offline fallback
When the Service Worker is active and content is cached, the app SHALL be able to serve cached content when offline.

#### Scenario: Cached assets served offline
- **WHEN** the app has previously loaded with Service Worker active and cached assets
- **AND** the network is unavailable
- **THEN** navigation to the SPA root SHALL serve cached content
- **AND** the app SHALL remain usable for cached routes and assets

### Requirement: Documentation
Documentation SHALL describe how to enable Service Worker, configure cache strategies, and add custom Service Workers.

#### Scenario: Enablement steps are documented
- **WHEN** a developer consults the offline caching documentation
- **THEN** they SHALL find steps to enable Service Worker in SPA hosting configuration

#### Scenario: Strategy selection is documented
- **WHEN** a developer consults the offline caching documentation
- **THEN** they SHALL find guidance on when to use each cache strategy (e.g., cache-first for static assets, network-first for API)
