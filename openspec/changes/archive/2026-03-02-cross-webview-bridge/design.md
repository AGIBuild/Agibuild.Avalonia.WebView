## Context

Each WebView has its own bridge instance and JS context. WebViews cannot directly call each other's JS or C#. The host runtime has access to all WebView instances and can route messages. A mediator pattern allows the host to receive a message from WebView A, determine subscribers (WebView B, C, or C# handlers), and deliver.

**Gap**: No cross-WebView API exists. Multi-WebView apps must use ad-hoc solutions (e.g., C# as relay) or cannot coordinate.

## Goals / Non-Goals

**Goals:**
- Publish-subscribe: topic-based messaging; any WebView or C# can publish; subscribers in other WebViews or C# receive
- Request-response: WebView A sends request with topic/correlation; WebView B or C# responds
- JS API: `subscribe(topic, handler)`, `publish(topic, payload)`, `request(topic, payload)` returning Promise
- C# API: equivalent subscribe/publish/request for host participation

**Non-Goals:**
- Direct peer-to-peer without host (security: host must mediate)
- Strong ordering or transactional guarantees
- Binary payloads (JSON only for simplicity)

## Decisions

### D1: Mediator architecture

**Choice**: Host runtime maintains a `ICrossWebViewChannel` (or `IWebViewMediator`) that tracks subscriptions per topic. When a message is published, the mediator iterates subscribers and dispatches to their WebView's bridge (or C# handler). Each WebView registers with the channel on attach, unregisters on detach.

**Alternatives considered**:
- Central message bus in C# only, JS calls C# to publish/subscribe: Adds round-trips; C# becomes bottleneck
- WebSocket between WebViews: Requires separate transport; host mediation is simpler

**Rationale**: Host already has references to all WebViews; mediator is a natural extension. Single point of routing.

### D2: Topic naming and scoping

**Choice**: String topics (e.g., `"user:login"`, `"app:theme-changed"`). No hierarchy or wildcards initially. Optional namespace prefix for app-specific topics.

**Rationale**: Simple, flexible. Wildcards can be added later if needed.

### D3: Request-response correlation

**Choice**: Request includes a correlation ID; response includes the same ID. Mediator routes response back to the originating WebView's Promise resolver. Timeout configurable (e.g., 30s default).

**Rationale**: Standard request-response pattern; timeout prevents hung Promises.

## Risks / Trade-offs

- **[Risk] WebView lifecycle** → Subscriber WebView may be disposed before message delivery. Mediator must skip disposed WebViews and not hold strong references.
- **[Risk] Ordering** → No guarantee of delivery order across WebViews. Document as best-effort.
- **[Trade-off] JSON-only** → Binary would require base64 or separate API; JSON covers most use cases.
