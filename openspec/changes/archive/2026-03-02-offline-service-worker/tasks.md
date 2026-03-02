## 1. Configuration API

- [x] 1.1 Add `ServiceWorkerOptions` (or extend `SpaHostingOptions`) with Enable, ScriptPath, Scope, CacheStrategy
- [x] 1.2 Define cache strategy enum/options: CacheFirst, NetworkFirst, StaleWhileRevalidate
- [x] 1.3 Support optional per-URL-pattern overrides for strategy

## 2. Built-in Service Worker

- [x] 2.1 Create built-in Service Worker script implementing configurable strategies
- [x] 2.2 Embed or serve built-in script from app resources when ScriptPath not specified
- [x] 2.3 Implement cache-first, network-first, stale-while-revalidate for fetch interception

## 3. Registration Integration

- [x] 3.1 Inject or include registration logic when loading SPA content (production mode)
- [x] 3.2 Register Service Worker with configured scope and script path
- [x] 3.3 Make registration conditional on ServiceWorkerOptions.Enable and production mode (or explicit opt-in for dev)

## 4. Documentation

- [x] 4.1 Document enablement steps and configuration options
- [x] 4.2 Document cache strategy selection and custom Service Worker usage
