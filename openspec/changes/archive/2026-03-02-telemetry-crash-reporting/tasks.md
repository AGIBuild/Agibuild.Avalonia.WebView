## 1. Interface and Core

- [x] 1.1 Define `ITelemetryProvider` with TrackEvent, TrackMetric, TrackException
- [x] 1.2 Add bridge instrumentation middleware that records call metrics when provider is registered
- [x] 1.3 Emit service name, method name, direction, latency, success/failure for each bridge call
- [x] 1.4 Register provider via DI or WebView environment options; ensure opt-in (no provider = no instrumentation)

## 2. Built-in Providers

- [x] 2.1 Implement OpenTelemetry provider: export bridge spans/metrics to OTLP or configured exporter
- [x] 2.2 Implement Sentry provider: capture exceptions, optional bridge breadcrumbs
- [x] 2.3 Create optional NuGet packages for OpenTelemetry and Sentry integrations

## 3. Bridge JS API (optional)

- [x] 3.1 Expose `telemetry.trackEvent(name, properties)` and `telemetry.trackException(message, stack)` to JS
- [x] 3.2 Forward JS calls to registered C# provider

## 4. Documentation

- [x] 4.1 Document telemetry setup and provider configuration
- [x] 4.2 Document privacy considerations (no PII in default properties)
- [x] 4.3 Document OpenTelemetry and Sentry integration steps
