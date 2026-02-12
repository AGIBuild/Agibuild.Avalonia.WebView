# Bridge Security Spec

## Overview
Rate limiting and security controls for the Bridge service layer.

## Requirements

### RS-1: RateLimit configuration
- `RateLimit(int maxCalls, TimeSpan window)` value type
- Validates: maxCalls > 0, window > TimeSpan.Zero
- `BridgeOptions.RateLimit` optional per-service setting

### RS-2: Sliding-window enforcement
- When RateLimit is set on a service, each RPC handler is wrapped with a sliding-window rate limiter
- Calls exceeding the limit throw `WebViewRpcException(-32029, "Rate limit exceeded")`
- The window uses `Environment.TickCount64` for monotonic timing

### RS-3: SG + Reflection path support
- For source-generated services: `RateLimitingRpcWrapper` (IWebViewRpcService decorator) intercepts all Handle() calls
- For reflection-based services: direct handler wrapping via `WrapWithRateLimit`

### RS-4: WebViewRpcException code preservation
- `WebViewRpcService.DispatchRequestAsync` preserves `WebViewRpcException.Code` in error responses
- Non-RPC exceptions still use code `-32603`

## Test Coverage
- 5 CTs in `BridgeSecurityTests`
