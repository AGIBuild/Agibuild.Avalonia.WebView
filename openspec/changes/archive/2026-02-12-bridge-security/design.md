# bridge-security — Design

**ROADMAP**: Phase 1, Deliverable 1.4

## Overview

`RateLimit(maxCalls, window)` is added to `BridgeOptions.RateLimit`. `RuntimeBridgeService` uses `RateLimitingRpcWrapper` — an `IWebViewRpcService` decorator — for the Source Generator path and direct handler wrapping for the reflection path. Both paths enforce sliding-window rate limiting per exposed service.

## Architecture

```
Expose<T>(impl, BridgeOptions { RateLimit })
         │
         ▼
┌─────────────────────────────────────┐
│  SG path: RateLimitingRpcWrapper    │
│  wraps IWebViewRpcService calls     │
│  reflection path: per-handler       │
│  wrapper invokes limiter            │
└─────────────────────────────────────┘
         │
         ▼
   Sliding-window: track call timestamps,
   reject when maxCalls exceeded in window
         │
         ▼
   WebViewRpcException(-32029) for throttled
```

## Key Details

- **RateLimit class**: `MaxCalls`, `Window` (TimeSpan). Constructor validates positive values.
- **Throttled response**: JSON-RPC error with `code: -32029`, `message: "Rate limit exceeded"`.
- **Error code preservation**: `WebViewRpcService.DispatchRequestAsync` fixed to preserve `WebViewRpcException.Code` when re-throwing.

## Testing

5 Contract Tests in `BridgeSecurityTests`:
- RateLimit constructor validation
- BridgeOptions.RateLimit settable
- Calls within limit succeed
- Calls exceeding limit return -32029
- Service without rate limit allows unlimited calls
