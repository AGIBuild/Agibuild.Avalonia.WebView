## 1. Core Pipeline

- [x] 1.1 Add `use(middleware: BridgeMiddleware): void` method to `BridgeClient` interface and implementation
- [x] 1.2 Define `BridgeMiddleware` type: `(context: BridgeCallContext, next: () => Promise<unknown>) => Promise<unknown>`
- [x] 1.3 Define `BridgeCallContext` interface: `serviceName`, `methodName`, `params`, `startedAt`, `properties: Map<string, unknown>`
- [x] 1.4 Implement middleware chain executor (onion model) that wraps the actual RPC `invoke` call as the innermost function
- [x] 1.5 Parse service name and method name from RPC method string (`"Service.method"`) to populate context
- [x] 1.6 Add unit tests: single middleware, multiple middleware ordering, no middleware passthrough, context population

## 2. Short-Circuit and Error Handling

- [x] 2.1 Implement short-circuit support: middleware returning without `next()` bypasses downstream
- [x] 2.2 Implement error propagation: middleware throwing prevents downstream execution
- [x] 2.3 Add unit tests: short-circuit returns cached value, short-circuit throws error, error from inner middleware propagates outward

## 3. Built-in Middlewares

- [x] 3.1 Implement `withLogging(options?)` — log service.method, params (truncated to configurable length), latency, errors to `console.log`/custom logger
- [x] 3.2 Implement `withTimeout(ms)` — race RPC call against timer, reject with `BridgeTimeoutError`
- [x] 3.3 Implement `withRetry({ maxRetries, delay, retryOn? })` — retry loop with delay and optional error filter
- [x] 3.4 Implement `withErrorNormalization()` — catch RPC errors, wrap in typed `BridgeError` class
- [x] 3.5 Define `BridgeError` and `BridgeTimeoutError` classes with typed properties
- [x] 3.6 Add unit tests for each built-in middleware in isolation

## 4. TypeScript Declarations and Exports

- [x] 4.1 Export all new types from `packages/bridge/src/index.ts`: `BridgeMiddleware`, `BridgeCallContext`, `BridgeError`, `BridgeTimeoutError`, all `with*` functions
- [x] 4.2 Update `packages/bridge/dist/index.d.ts` via `tsc` build
- [x] 4.3 Bump `@agibuild/bridge` package version (minor)

## 5. Integration and Samples

- [x] 5.1 Add integration test: bridge client with logging middleware → call C# service → verify middleware executed
- [x] 5.2 Add middleware usage example to `samples/avalonia-react/` (logging + error normalization in development)
- [x] 5.3 Update sample `bridge/services.ts` to show middleware-enabled client setup
