## Why

SPA-hosted apps in Fulora load content from embedded resources or a dev server. When offline or on poor connectivity, users get blank pages or load failures. Service Workers enable offline caching: assets and API responses can be cached and served when the network is unavailable. Fulora does not currently integrate Service Worker registration or provide configuration for cache strategies.

**Goal alignment**: Improve offline experience for SPA-hosted apps; enable PWA-like behavior in hybrid Fulora apps; provide a configuration API for cache strategies.

## What Changes

- Integrate Service Worker support for SPA-hosted apps
- Provide a configuration API for cache strategies (e.g., cache-first, network-first, stale-while-revalidate)
- Automatically register the Service Worker when loading SPA content from embedded resources or production build
- Support custom Service Worker scripts or use a built-in default that implements configurable strategies
- Document how to add a custom Service Worker and configure caching for app shell and API calls

## Non-goals

- Full PWA support (manifest, install prompt) — focus on offline caching only
- Service Worker in dev mode (optional; dev server typically has different caching needs)
- Modifying core bridge or hosting for Service Worker–specific behavior beyond registration

## Capabilities

### New Capabilities
- `offline-caching`: Service Worker integration for offline caching in SPA-hosted apps with configurable cache strategies

## Impact

- SPA hosting: Service Worker registration during navigation to SPA content
- Configuration: `SpaHostingOptions` or equivalent extended with `ServiceWorkerOptions` (strategy, scope, script path)
- New or extended package: runtime or hosting layer
- Documentation: Offline caching setup and strategy selection guide
