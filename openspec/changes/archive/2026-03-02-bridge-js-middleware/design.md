## Context

The C# bridge runtime has `IBridgeMiddleware` with `InvokeAsync(BridgeCallContext, BridgeCallDelegate)` — an ASP.NET-style pipeline for cross-cutting concerns on the host side. The JS side (`@agibuild/bridge` npm package) is a thin client exposing `bridgeClient.ready()`, `invoke()`, and `getService()` over `window.agWebView.rpc`. It has no middleware or interceptor capability.

This means frontend developers cannot add logging, retry, timeout, or error normalization to bridge calls without wrapping every service method manually.

The `@agibuild/bridge` package is published to npm and used by all frontend frameworks (React, Vue, Svelte, Angular, Vanilla).

## Goals / Non-Goals

**Goals:**
- Add a typed middleware pipeline to `@agibuild/bridge` with `use(middleware)` API on `BridgeClient`
- Follow the onion model (consistent with C# `IBridgeMiddleware` semantics)
- Provide built-in middleware library: logging, retry, timeout, error normalization
- Full TypeScript type safety for middleware context and composition
- Backward compatible — `bridgeClient` without `use()` works exactly as before

**Non-Goals:**
- Modifying C# `IBridgeMiddleware` — it's already complete
- Request/response transformation (param rewriting) — middleware observes, can reject, but does not transform payloads
- Middleware for `[JsImport]` (C#→JS) calls — future consideration
- Server-side middleware configuration from JS — C# middleware is configured in C#

## Decisions

### D1: Middleware signature

**Choice**: `(context: BridgeCallContext, next: () => Promise<unknown>) => Promise<unknown>`

**Rationale**: Matches the onion model pattern used in Express, Koa, and C# `IBridgeMiddleware`. The `next()` function invokes the next middleware in the chain (or the actual RPC call at the end). Middleware can execute logic before and after `next()`.

### D2: BridgeCallContext shape

**Choice**:
```typescript
interface BridgeCallContext {
  readonly serviceName: string;
  readonly methodName: string;
  readonly params: Record<string, unknown> | undefined;
  readonly startedAt: number; // Date.now()
  properties: Map<string, unknown>; // extensible bag
}
```

**Rationale**: `serviceName` and `methodName` are parsed from the RPC method string (`"Service.method"`). `startedAt` enables latency measurement. `properties` bag allows middleware to pass data downstream (e.g., correlation ID).

### D3: Registration API

**Choice**: `bridgeClient.use(middleware)` for global middleware. `bridgeClient.getService<T>(name, { middleware: [...] })` for per-service middleware (optional future extension, not in initial scope).

**Rationale**: Global middleware covers 90% of use cases (logging, timeout). Per-service middleware is an optimization for later. `use()` must be called before `ready()` or service calls to ensure consistent pipeline.

### D4: Built-in middleware

**Choice**: Ship 4 built-in middlewares:
1. `withLogging(options?)` — logs service.method, params (truncated), latency, errors
2. `withTimeout(ms)` — rejects with `TimeoutError` if call exceeds duration
3. `withRetry({ maxRetries, delay, retryOn })` — retry on specific error conditions
4. `withErrorNormalization()` — wraps raw RPC errors into typed `BridgeError` class

**Rationale**: These are the most common cross-cutting concerns. Each is independently usable and composable.

### D5: Execution order

**Choice**: Middlewares execute in registration order (first registered = outermost). This means the last `use()` call is the innermost middleware, closest to the actual RPC call.

**Rationale**: Matches Koa/Express middleware ordering convention.

## Testing Strategy

- **Unit tests**: Middleware pipeline execution order, context population, error propagation, short-circuiting
- **Unit tests**: Each built-in middleware in isolation (timeout triggers, retry counts, logging output, error normalization)
- **Integration tests**: Bridge client with middleware → call C# service → verify middleware executed (logging, timing)

## Risks / Trade-offs

- **[Performance overhead]** Each middleware adds a function call to the chain → Mitigation: function call overhead is negligible compared to RPC serialization + WebView message passing (~1ms+)
- **[Breaking change risk]** Extending `BridgeClient` interface → Mitigation: `use()` is additive, no existing API changes. npm minor version bump.
- **[Middleware ordering confusion]** Users may expect LIFO vs FIFO → Mitigation: document clearly with examples; follow widely-known Koa convention
