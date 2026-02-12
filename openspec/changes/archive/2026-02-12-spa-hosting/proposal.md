# spa-hosting

**Goal**: G2
**ROADMAP**: Phase 2, Deliverables 2.1 + 2.2 + 2.3 + 2.4

## Problem

No built-in way to serve SPA content from embedded resources or dev servers. Users must manually wire custom schemes and resource interception.

## Proposed Solution

Provide `SpaHostingOptions` + `SpaHostingService` + `WebViewCore.EnableSpaHosting()`:

- **SpaHostingOptions**: Configures scheme, host, fallback document, embedded resource prefix, resource assembly, dev server URL, auto-inject bridge script, default headers.
- **SpaHostingService**: Handles `WebResourceRequested` — serves embedded resources via custom scheme (`app://`), supports SPA router fallback (no extension → index.html), dev server proxy mode via `HttpClient`, auto-injects bridge script.
- **WebViewCore.EnableSpaHosting()**: Registers custom scheme, subscribes to `WebResourceRequested`, auto-enables bridge.

## References

- [PROJECT.md](../../PROJECT.md) — G2
- [ROADMAP.md](../../ROADMAP.md) — Deliverables 2.1, 2.2, 2.3, 2.4
