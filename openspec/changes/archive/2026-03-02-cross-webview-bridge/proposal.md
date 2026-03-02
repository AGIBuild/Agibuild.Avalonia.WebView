## Why

Agibuild.Fulora supports multiple WebView instances in an app (e.g., main window + dialogs, or split views), but each WebView has an isolated bridge. There is no way for WebView A to communicate with WebView B. Use cases include: syncing state across views, broadcasting events (e.g., "user logged in"), or request-response between different SPA routes hosted in separate WebViews.

**Goal alignment**: Enable multi-WebView apps to share state and coordinate; support publish-subscribe and request-response patterns via the host as mediator.

## What Changes

- Add a cross-WebView communication channel mediated by the host runtime
- Support publish-subscribe: any WebView can publish to a topic; subscribers in other WebViews receive the message
- Support request-response: WebView A can send a request that WebView B (or host) responds to
- Expose a JS API for subscribing, publishing, and requesting; C# can also participate as publisher/subscriber
- Messages are JSON-serializable; topics are string identifiers

## Non-goals

- Direct WebView-to-WebView messaging without host mediation (security and lifecycle reasons)
- Replacing the existing single-WebView bridge; cross-WebView is additive
- Guaranteeing delivery order or exactly-once semantics across WebViews (best-effort)

## Capabilities

### New Capabilities
- `webview-inter-communication`: Publish-subscribe and request-response communication between multiple WebViews via host runtime as mediator

## Impact

- Core runtime: New `ICrossWebViewChannel` (or equivalent) and mediator service
- Bridge: New JS API for `subscribe`, `publish`, `request`/`respond`
- Samples: Optional demo of multi-WebView communication
