## Purpose

Define cross-WebView communication capabilities for Agibuild.Fulora, enabling publish-subscribe and request-response patterns between multiple WebView instances via the host runtime as mediator.

## ADDED Requirements

### Requirement: Publish-subscribe channel
Runtime SHALL provide a publish-subscribe channel where any WebView or C# can publish to a topic and subscribers in other WebViews or C# receive the message.

#### Scenario: WebView publishes to topic
- **WHEN** JS in WebView A calls `publish(topic, payload)` with a string topic and JSON-serializable payload
- **THEN** the host mediator SHALL deliver the payload to all subscribers of that topic (other WebViews and C# handlers)
- **AND** the publishing WebView SHALL NOT receive its own message (unless it is also a subscriber)

#### Scenario: WebView subscribes to topic
- **WHEN** JS in WebView B calls `subscribe(topic, handler)` with a topic and callback
- **THEN** the handler SHALL be invoked when any other WebView or C# publishes to that topic
- **AND** the handler SHALL receive the payload as an argument

#### Scenario: C# can publish and subscribe
- **WHEN** C# code calls the equivalent publish/subscribe API
- **THEN** C# SHALL be able to publish to topics and receive messages from WebViews
- **AND** C# subscribers SHALL receive the same payload format as JS subscribers

### Requirement: Request-response pattern
Runtime SHALL support request-response where a WebView (or C#) sends a request and receives a response from another WebView or C#.

#### Scenario: WebView sends request and receives response
- **WHEN** JS in WebView A calls `request(topic, payload)` returning a Promise
- **THEN** the mediator SHALL route the request to a responder (WebView B or C#) registered for that topic
- **AND** the Promise SHALL resolve with the response payload when the responder replies
- **AND** the Promise SHALL reject on timeout (configurable, e.g., 30s default) or if no responder exists

#### Scenario: WebView or C# responds to request
- **WHEN** JS or C# registers as a responder for a topic with `respond(topic, handler)`
- **THEN** the handler SHALL be invoked when a request is received for that topic
- **AND** the handler SHALL return a value (or Promise) that is sent back as the response

### Requirement: Channel lifecycle
The cross-WebView channel SHALL respect WebView lifecycle: subscribers and responders are automatically unregistered when their WebView is detached or disposed.

#### Scenario: Subscriber WebView disposed
- **WHEN** a WebView that has subscribed to topics is detached or disposed
- **THEN** its subscriptions SHALL be removed from the mediator
- **AND** the mediator SHALL NOT hold strong references to the disposed WebView

#### Scenario: Mediator scoped to app
- **WHEN** the application has multiple WebView instances
- **THEN** the channel SHALL be shared across all WebViews in the same host process
- **AND** each WebView SHALL be able to participate once attached

### Requirement: JS API surface
The bridge SHALL expose a JS API for cross-WebView communication.

#### Scenario: subscribe, publish, request available in JS
- **WHEN** the bridge is initialized in a WebView
- **THEN** `window.agWebView` (or equivalent) SHALL expose `crossWebView.subscribe(topic, handler)`, `crossWebView.publish(topic, payload)`, and `crossWebView.request(topic, payload)` (returning Promise)

#### Scenario: Payload is JSON-serializable
- **WHEN** a payload is published or sent in a request
- **THEN** it SHALL be JSON-serializable (primitives, objects, arrays)
- **AND** non-JSON-serializable values SHALL be rejected or serialized per platform conventions
