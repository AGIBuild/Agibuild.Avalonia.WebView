## Context

Fulora's SPA hosting serves content from `app://localhost/` (embedded resources) or a dev server. The WebView's underlying engine (WebView2, WebKit, etc.) supports Service Workers. A Service Worker can intercept fetch requests and serve cached responses when offline. Registration is done via `navigator.serviceWorker.register(scriptUrl, { scope })`.

**Gap**: No Service Worker registration or configuration exists. Apps must manually inject a SW script and configure caching, which is error-prone and inconsistent.

## Goals / Non-Goals

**Goals:**
- Automatic Service Worker registration when loading SPA content (production mode)
- Configuration API for cache strategies (cache-first, network-first, stale-while-revalidate)
- Support custom Service Worker script or built-in default
- Document setup and strategy selection

**Non-Goals:**
- Full PWA (manifest, install prompt)
- Service Worker in dev mode by default (optional)
- Background sync or push notifications

## Decisions

### D1: Registration trigger

**Choice**: Register Service Worker when the WebView navigates to the SPA root (e.g., `app://localhost/` or `app://localhost/index.html`). Registration happens from within the loaded page (injected script or page script) so `navigator.serviceWorker` is available.

**Alternatives considered**:
- Preload script: May run before document is ready; SW registration typically needs document context
- C#-driven registration: Not possible; SW is a web API, must be called from JS

**Rationale**: Registration from the SPA's own script (or an injected bootstrap) is standard. Host can ensure the script runs on first load.

### D2: Built-in vs. custom Service Worker

**Choice**: Support both. Provide a built-in default Service Worker script that implements configurable strategies (cache-first for assets, network-first for API, etc.). Allow override via `ServiceWorkerOptions.ScriptPath` for custom SW.

**Rationale**: Built-in covers common cases; custom allows advanced scenarios.

### D3: Cache strategy configuration

**Choice**: `ServiceWorkerOptions` with `CacheStrategy` enum or object: `CacheFirst`, `NetworkFirst`, `StaleWhileRevalidate`. Optional per-URL-pattern overrides (e.g., `/api/*` → network-first, `*.js` → cache-first).

**Rationale**: Simple API; extensible for pattern-based overrides.

## Risks / Trade-offs

- **[Risk] WebView Service Worker support** → WebView2 and WebKit support SW; verify on all platforms. Some embedded WebViews may have limitations.
- **[Risk] Scope and update** → SW scope affects which requests are intercepted. Document scope selection. Updates (new SW version) may need app reload.
- **[Trade-off] Dev mode** → Default off in dev; optional enable for testing offline behavior.
