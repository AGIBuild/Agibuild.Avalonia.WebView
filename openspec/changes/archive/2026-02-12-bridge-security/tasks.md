# bridge-security — Tasks

## Task 1: Add RateLimit class + BridgeOptions.RateLimit
**Acceptance**: `RateLimit(maxCalls, window)` in Core; `BridgeOptions.RateLimit` property; constructor validates positive values.

## Task 2: Implement WrapWithRateLimit + RateLimitingRpcWrapper
**Acceptance**: `RateLimitingRpcWrapper` implements `IWebViewRpcService`; sliding-window enforcement; throttled calls throw `WebViewRpcException(-32029)`.

## Task 3: Wire into Expose (SG + reflection)
**Acceptance**: SG path uses `RateLimitingRpcWrapper`; reflection path wraps handlers with rate limiter; both paths respect `BridgeOptions.RateLimit`.

## Task 4: Fix WebViewRpcService error code preservation
**Acceptance**: `DispatchRequestAsync` preserves `WebViewRpcException.Code` when propagating errors.

## Task 5: Write 5 tests
**Acceptance**: `BridgeSecurityTests` — constructor validation, BridgeOptions wiring, within-limit, exceeding-limit, no-limit-unlimited.
