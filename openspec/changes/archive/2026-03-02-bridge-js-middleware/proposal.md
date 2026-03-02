## Why

The C# side of the bridge has `IBridgeMiddleware` for request interception (logging, auth, caching), but the JS side (`@agibuild/bridge` npm package) is a thin client with no extension points. Frontend developers cannot add cross-cutting concerns like request logging, error normalization, retry logic, or authentication token injection without wrapping every service call manually. This asymmetry limits the bridge's value as a full-stack typed RPC framework.

**Goal IDs**: G1 (Type-Safe Bridge — completing the middleware story on both sides), E2 (Dev Tooling — JS-side logging/tracing)

**ROADMAP justification**: Post-1.0 differentiation. Neither Electron's IPC nor Tauri's commands have a typed middleware pipeline on the frontend side. Adding this makes Fulora's bridge uniquely powerful for enterprise-grade hybrid applications.

## What Changes

- Add interceptor/middleware API to `@agibuild/bridge` package: `use(middleware)` on `BridgeClient`
- Define `BridgeMiddleware` type: `(context: BridgeCallContext, next: () => Promise<unknown>) => Promise<unknown>`
- Provide built-in middlewares: `withLogging()`, `withRetry(options)`, `withTimeout(ms)`, `withErrorNormalization()`
- `BridgeCallContext` exposes: `serviceName`, `methodName`, `params`, `startedAt`, plus extensible `properties` map
- Middleware executes in registration order (onion model), consistent with C# `IBridgeMiddleware` semantics
- Full TypeScript typing for middleware context and composition

## Non-goals

- Modifying C# `IBridgeMiddleware` — already complete
- Request/response transformation (params rewriting) — middleware observes and can reject, not transform
- Server-side (C#→JS) middleware for `[JsImport]` calls — future consideration

## Capabilities

### New Capabilities
- `bridge-js-middleware`: TypeScript middleware pipeline for `@agibuild/bridge` client with typed context, onion-model execution, and built-in middleware library (logging, retry, timeout, error normalization)

### Modified Capabilities
- None (C# bridge contracts unchanged)

## Impact

- `packages/bridge/` — `src/middleware.ts`, updated `BridgeClient` interface, built-in middleware implementations
- `packages/bridge/dist/` — updated type declarations
- `samples/avalonia-react/` — demonstrate middleware usage (logging, error handling)
- `tests/` — unit tests for middleware pipeline, ordering, error propagation
- npm publication — minor version bump for `@agibuild/bridge`
