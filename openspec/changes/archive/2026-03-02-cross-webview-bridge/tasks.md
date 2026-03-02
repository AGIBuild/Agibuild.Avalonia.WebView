## 1. Core Mediator

- [x] 1.1 Define `ICrossWebViewChannel` (or `IWebViewMediator`) interface with Subscribe, Unsubscribe, Publish, Request, Respond methods
- [x] 1.2 Implement mediator that tracks subscriptions and responders per topic, keyed by WebView/bridge instance
- [x] 1.3 Implement lifecycle: register WebView on attach, unregister on detach; avoid strong references to disposed WebViews
- [x] 1.4 Implement request-response correlation (correlation ID, timeout, Promise resolution)

## 2. Bridge Integration

- [x] 2.1 Add JS API: `crossWebView.subscribe(topic, handler)`, `crossWebView.publish(topic, payload)`, `crossWebView.request(topic, payload)`
- [x] 2.2 Add C# API for publish, subscribe, request, respond
- [x] 2.3 Wire bridge to mediator when WebView attaches; route incoming messages to correct WebView's JS context

## 3. Configuration and Documentation

- [x] 3.1 Add configuration for default request timeout
- [x] 3.2 Document topic naming conventions and usage patterns
- [x] 3.3 Add optional sample demonstrating multi-WebView communication
